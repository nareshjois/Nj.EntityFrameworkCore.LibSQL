using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nj.LibSql.Data.Exceptions;

namespace Nj.LibSql.Data.Http;

/// <summary>
/// Hrana-over-WebSocket session (Hrana 3 JSON). Used for <c>libsql://</c>, <c>wss://</c>, and <c>ws://</c>.
/// </summary>
internal sealed class LibSqlWsClient : ILibSqlHranaSession
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ClientWebSocket _socket = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string? _jwt;
    private int _nextRequestId = 1;
    private int _streamId = 1;
    private bool _streamOpen;
    private bool _disposed;

    private LibSqlWsClient(string? jwt)
        => _jwt = string.IsNullOrWhiteSpace(jwt) ? null : jwt;

    /// <summary>ConnectAsync(.</summary>
    public static async Task<LibSqlWsClient> ConnectAsync(
        string url,
        string? authToken,
        CancellationToken cancellationToken = default)
    {
        var client = new LibSqlWsClient(authToken);
        try
        {
            await client.ConnectCoreAsync(NormalizeWsUrl(url), cancellationToken).ConfigureAwait(false);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private async Task ConnectCoreAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        _socket.Options.AddSubProtocol("hrana3");
        _socket.Options.AddSubProtocol("hrana2");
        _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        await _socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

        await SendJsonAsync(
            new { type = "hello", jwt = _jwt },
            cancellationToken).ConfigureAwait(false);

        using var hello = await ReceiveJsonAsync(cancellationToken).ConfigureAwait(false);
        var type = hello.RootElement.GetProperty("type").GetString();
        if (type == "hello_error")
        {
            var message = hello.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "WebSocket hello failed";
            throw new LibSqlConnectionException(message ?? "WebSocket hello failed");
        }

        if (type != "hello_ok")
        {
            throw new LibSqlException($"Unexpected WebSocket hello response type '{type}'.");
        }
    }

    /// <summary>TestConnectionAsync(CancellationToken.</summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = new HranaBatchRequest();
            batch.Requests.Add(new HranaRequest
            {
                Type = HranaTypes.Execute,
                Statement = new HranaStatement { Sql = "SELECT 1", Args = null },
            });
            var response = await ExecuteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            return response.Results.Count > 0 && response.Results[0].Type == HranaTypes.Ok;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>ExecuteBatchAsync(.</summary>
    public async Task<HranaBatchResponse> ExecuteBatchAsync(
        HranaBatchRequest batch,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(batch);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await EnsureStreamOpenAsync(cancellationToken).ConfigureAwait(false);

            var results = new List<HranaResult>();
            foreach (var request in batch.Requests)
            {
                results.Add(await ExecutePipelineRequestAsync(request, cancellationToken).ConfigureAwait(false));
            }

            return new HranaBatchResponse { Results = results, Baton = null };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Runs a SELECT (or other query) via Hrana 3 cursor streaming — preferred for large result sets.
    /// </summary>
    public async Task<HranaQueryResult> ExecuteCursorQueryAsync(
        string sql,
        List<HranaValue>? args,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureStreamOpenAsync(cancellationToken).ConfigureAwait(false);
            var cursorId = _nextRequestId; // reuse id space; unique per connection while open
            var openId = NextRequestId();

            var batch = new
            {
                type = "batch",
                steps = new object[]
                {
                    new
                    {
                        stmt = new
                        {
                            sql,
                            args = ToWsArgs(args),
                            want_rows = true,
                        },
                    },
                },
            };

            await SendJsonAsync(
                new
                {
                    type = "request",
                    request_id = openId,
                    request = new
                    {
                        type = "open_cursor",
                        stream_id = _streamId,
                        cursor_id = cursorId,
                        batch,
                    },
                },
                cancellationToken).ConfigureAwait(false);

            using var openResp = await WaitForResponseAsync(openId, cancellationToken).ConfigureAwait(false);
            EnsureResponseOk(openResp, "open_cursor");

            var cols = new List<string>();
            var rows = new List<List<HranaValue>>();
            ulong affected = 0;
            long lastRowId = 0;
            var done = false;

            while (!done)
            {
                var fetchId = NextRequestId();
                await SendJsonAsync(
                    new
                    {
                        type = "request",
                        request_id = fetchId,
                        request = new
                        {
                            type = "fetch_cursor",
                            cursor_id = cursorId,
                            max_count = 256u,
                        },
                    },
                    cancellationToken).ConfigureAwait(false);

                using var fetchDoc = await WaitForResponseAsync(fetchId, cancellationToken).ConfigureAwait(false);
                var fetch = EnsureResponseOk(fetchDoc, "fetch_cursor");
                done = fetch.TryGetProperty("done", out var doneEl) && doneEl.GetBoolean();

                if (fetch.TryGetProperty("entries", out var entries))
                {
                    foreach (var entry in entries.EnumerateArray())
                    {
                        var entryType = entry.GetProperty("type").GetString();
                        if (entryType == "step_begin"
                            && entry.TryGetProperty("cols", out var colEls))
                        {
                            cols.Clear();
                            foreach (var col in colEls.EnumerateArray())
                            {
                                cols.Add(
                                    col.TryGetProperty("name", out var nameEl)
                                        ? nameEl.GetString() ?? string.Empty
                                        : string.Empty);
                            }
                        }
                        else if (entryType == "row"
                                 && entry.TryGetProperty("row", out var rowEl))
                        {
                            var values = new List<HranaValue>();
                            foreach (var cell in rowEl.EnumerateArray())
                            {
                                values.Add(ParseWsValue(cell));
                            }

                            rows.Add(values);
                        }
                        else if (entryType == "step_end"
                                 && entry.TryGetProperty("affected_row_count", out var arc))
                        {
                            affected = arc.GetUInt64();
                            if (entry.TryGetProperty("last_insert_rowid", out var lir)
                                && lir.ValueKind == JsonValueKind.String
                                && long.TryParse(lir.GetString(), out var parsed))
                            {
                                lastRowId = parsed;
                            }
                        }
                        else if (entryType == "step_error")
                        {
                            var message = entry.TryGetProperty("error", out var err)
                                && err.TryGetProperty("message", out var msg)
                                ? msg.GetString()
                                : "Cursor step failed";
                            throw new LibSqlException($"SQL Error: {message}");
                        }
                    }
                }
            }

            var closeId = NextRequestId();
            await SendJsonAsync(
                new
                {
                    type = "request",
                    request_id = closeId,
                    request = new { type = "close_cursor", cursor_id = cursorId },
                },
                cancellationToken).ConfigureAwait(false);
            using var closeDoc = await WaitForResponseAsync(closeId, cancellationToken).ConfigureAwait(false);
            EnsureResponseOk(closeDoc, "close_cursor");

            return new HranaQueryResult
            {
                Cols = cols.Select(n => new HranaColumn { Name = n }).ToList(),
                Rows = rows,
                AffectedRowCount = affected,
                LastInsertRowid = lastRowId.ToString(),
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<HranaResult> ExecutePipelineRequestAsync(
        HranaRequest request,
        CancellationToken cancellationToken)
    {
        var requestId = NextRequestId();
        object wsRequest = request.Type switch
        {
            var t when t == HranaTypes.Execute => new
            {
                type = "execute",
                stream_id = _streamId,
                stmt = new
                {
                    sql = request.Statement?.Sql,
                    args = ToWsArgs(request.Statement?.Args),
                    want_rows = true,
                },
            },
            var t when t == HranaTypes.Batch => new
            {
                type = "batch",
                stream_id = _streamId,
                batch = ToWsBatch(request.Batch),
            },
            var t when t == HranaTypes.Close => new
            {
                type = "close_stream",
                stream_id = _streamId,
            },
            _ => throw new NotSupportedException($"Hrana request type '{request.Type}' is not supported over WebSocket."),
        };

        await SendJsonAsync(
            new { type = "request", request_id = requestId, request = wsRequest },
            cancellationToken).ConfigureAwait(false);

        using var doc = await WaitForResponseAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (doc.RootElement.GetProperty("type").GetString() == "response_error")
        {
            var message = doc.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "WebSocket request failed";
            throw new LibSqlException($"SQL Error: {message}");
        }

        var response = doc.RootElement.GetProperty("response");
        var respType = response.GetProperty("type").GetString();

        if (respType == "close_stream")
        {
            _streamOpen = false;
            return new HranaResult { Type = HranaTypes.Ok };
        }

        if (respType == "execute")
        {
            var result = ParseStmtResult(response.GetProperty("result"));
            return new HranaResult
            {
                Type = HranaTypes.Ok,
                Response = new HranaResponse { Type = HranaTypes.Execute, Result = result },
            };
        }

        if (respType == "batch")
        {
            return new HranaResult
            {
                Type = HranaTypes.Ok,
                Response = new HranaResponse
                {
                    Type = HranaTypes.Batch,
                    BatchResult = ParseBatchResult(response.GetProperty("result")),
                },
            };
        }

        throw new LibSqlException($"Unexpected WebSocket response type '{respType}'.");
    }

    private async Task EnsureStreamOpenAsync(CancellationToken cancellationToken)
    {
        if (_streamOpen)
        {
            return;
        }

        var requestId = NextRequestId();
        await SendJsonAsync(
            new
            {
                type = "request",
                request_id = requestId,
                request = new { type = "open_stream", stream_id = _streamId },
            },
            cancellationToken).ConfigureAwait(false);

        using var doc = await WaitForResponseAsync(requestId, cancellationToken).ConfigureAwait(false);
        EnsureResponseOk(doc, "open_stream");
        _streamOpen = true;
    }

    private int NextRequestId() => Interlocked.Increment(ref _nextRequestId);

    private async Task SendJsonAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<JsonDocument> ReceiveJsonAsync(CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            using var ms = new MemoryStream();
            ValueWebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new LibSqlConnectionException("WebSocket closed by server.");
                }

                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            ms.Position = 0;
            return await JsonDocument.ParseAsync(ms, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task<JsonDocument> WaitForResponseAsync(int requestId, CancellationToken cancellationToken)
    {
        // Responses may arrive out of order; loop until matching request_id.
        while (true)
        {
            var doc = await ReceiveJsonAsync(cancellationToken).ConfigureAwait(false);
            var type = doc.RootElement.GetProperty("type").GetString();
            if (type is not ("response_ok" or "response_error"))
            {
                doc.Dispose();
                continue;
            }

            if (doc.RootElement.GetProperty("request_id").GetInt32() == requestId)
            {
                return doc;
            }

            // Unmatched response — discard for MVP (single outstanding request under lock).
            doc.Dispose();
        }
    }

    private static JsonElement EnsureResponseOk(JsonDocument doc, string expectedType)
    {
        if (doc.RootElement.GetProperty("type").GetString() == "response_error")
        {
            var message = doc.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : $"{expectedType} failed";
            throw new LibSqlException($"SQL Error: {message}");
        }

        var response = doc.RootElement.GetProperty("response");
        var type = response.GetProperty("type").GetString();
        if (type != expectedType)
        {
            throw new LibSqlException($"Expected '{expectedType}' response, got '{type}'.");
        }

        return response;
    }

    private static object? ToWsArgs(List<HranaValue>? args)
    {
        if (args is null || args.Count == 0)
        {
            return null;
        }

        var result = new object[args.Count];
        for (var i = 0; i < args.Count; i++)
        {
            var a = args[i];
            result[i] = a.Type switch
            {
                var t when t == HranaTypes.Null => new Dictionary<string, object?> { ["type"] = "null" },
                var t when t == HranaTypes.Integer => new Dictionary<string, object?>
                {
                    ["type"] = "integer",
                    ["value"] = a.Value?.ToString(),
                },
                var t when t == HranaTypes.Float => new Dictionary<string, object?>
                {
                    ["type"] = "float",
                    ["value"] = a.Value,
                },
                var t when t == HranaTypes.Text => new Dictionary<string, object?>
                {
                    ["type"] = "text",
                    ["value"] = a.Value?.ToString(),
                },
                var t when t == HranaTypes.Blob => new Dictionary<string, object?>
                {
                    ["type"] = "blob",
                    ["base64"] = a.Base64,
                },
                _ => new Dictionary<string, object?>
                {
                    ["type"] = "text",
                    ["value"] = a.Value?.ToString() ?? string.Empty,
                },
            };
        }

        return result;
    }

    private static object ToWsBatch(HranaBatch? batch)
    {
        var steps = batch?.Steps ?? [];
        return new
        {
            steps = steps.Select(s => new
            {
                stmt = new
                {
                    sql = s.Statement?.Sql,
                    args = ToWsArgs(s.Statement?.Args),
                    want_rows = true,
                },
                condition = s.Condition is null
                    ? null
                    : new { type = s.Condition.Type, step = s.Condition.Step },
            }).ToArray(),
        };
    }

    private static HranaQueryResult ParseStmtResult(JsonElement result)
    {
        var cols = new List<HranaColumn>();
        if (result.TryGetProperty("cols", out var colEls))
        {
            foreach (var col in colEls.EnumerateArray())
            {
                cols.Add(new HranaColumn
                {
                    Name = col.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty,
                });
            }
        }

        var rows = new List<List<HranaValue>>();
        if (result.TryGetProperty("rows", out var rowEls))
        {
            foreach (var row in rowEls.EnumerateArray())
            {
                var values = new List<HranaValue>();
                foreach (var cell in row.EnumerateArray())
                {
                    values.Add(ParseWsValue(cell));
                }

                rows.Add(values);
            }
        }

        ulong affected = 0;
        if (result.TryGetProperty("affected_row_count", out var arc))
        {
            affected = arc.GetUInt64();
        }

        string? lastInsert = null;
        if (result.TryGetProperty("last_insert_rowid", out var lir)
            && lir.ValueKind != JsonValueKind.Null)
        {
            lastInsert = lir.ValueKind == JsonValueKind.String ? lir.GetString() : lir.ToString();
        }

        return new HranaQueryResult
        {
            Cols = cols,
            Rows = rows,
            AffectedRowCount = affected,
            LastInsertRowid = lastInsert,
        };
    }

    private static HranaBatchResult ParseBatchResult(JsonElement result)
    {
        return new HranaBatchResult
        {
            StepResults = [],
            StepErrors = [],
        };
    }

    private static HranaValue ParseWsValue(JsonElement cell)
    {
        var type = cell.TryGetProperty("type", out var t) ? t.GetString() : "null";
        return type switch
        {
            "null" => new HranaValue { Type = HranaTypes.Null },
            "integer" => new HranaValue
            {
                Type = HranaTypes.Integer,
                Value = cell.TryGetProperty("value", out var iv) ? iv.ToString() : "0",
            },
            "float" => new HranaValue
            {
                Type = HranaTypes.Float,
                Value = cell.TryGetProperty("value", out var fv) ? fv.GetDouble() : 0d,
            },
            "text" => new HranaValue
            {
                Type = HranaTypes.Text,
                Value = cell.TryGetProperty("value", out var tv) ? tv.GetString() : string.Empty,
            },
            "blob" => new HranaValue
            {
                Type = HranaTypes.Blob,
                Base64 = cell.TryGetProperty("base64", out var b64) ? b64.GetString() : null,
            },
            _ => new HranaValue { Type = HranaTypes.Text, Value = cell.ToString() },
        };
    }

    internal static Uri NormalizeWsUrl(string url)
    {
        if (url.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase))
        {
            // Explicit WSS path only — callers should prefer https for libsql:// (Turso).
            url = string.Concat("wss://", url.AsSpan(9));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "ws" && uri.Scheme != "wss"))
        {
            throw new ArgumentException($"Invalid WebSocket URL: {url}", nameof(url));
        }

        // Self-hosted sqld accepts the WebSocket upgrade on "/".
        // "/v2" and "/v3" are HTTP Hrana endpoints and return 200 text, not 101.
        var builder = new UriBuilder(uri);
        if (string.IsNullOrEmpty(builder.Path) || builder.Path == "/")
        {
            builder.Path = "/";
        }

        return builder.Uri;
    }

    /// <summary>Dispose().</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
        }
        catch
        {
            // Best-effort close.
        }

        _socket.Dispose();
        _lock.Dispose();
    }
}
