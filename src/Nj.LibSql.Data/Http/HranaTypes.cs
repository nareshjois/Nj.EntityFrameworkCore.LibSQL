#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nj.LibSql.Data.Http;

/// <summary>
/// Represents a Hrana protocol batch request.
/// </summary>
internal sealed class HranaBatchRequest
{
    /// <summary>Baton.</summary>
    [JsonPropertyName("baton")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Baton { get; set; }

    /// <summary>Requests.</summary>
    [JsonPropertyName("requests")]
    public List<HranaRequest> Requests { get; set; } = new();
}

/// <summary>
/// Represents a single request in a Hrana batch.
/// </summary>
internal sealed class HranaRequest
{
    /// <summary>Type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>Statement.</summary>
    [JsonPropertyName("stmt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaStatement? Statement { get; set; }

    /// <summary>Batch.</summary>
    [JsonPropertyName("batch")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatch? Batch { get; set; }

    /// <summary>Sql.</summary>
    [JsonPropertyName("sql")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sql { get; set; }
}

/// <summary>
/// Represents a SQL statement in a Hrana request.
/// </summary>
internal sealed class HranaStatement
{
    /// <summary>Sql.</summary>
    [JsonPropertyName("sql")]
    public string Sql { get; set; }

    /// <summary>Args.</summary>
    [JsonPropertyName("args")]
    public List<HranaValue>? Args { get; set; }
}

/// <summary>
/// Represents a parameter value in Hrana protocol.
/// </summary>
[JsonConverter(typeof(HranaValueJsonConverter))]
internal sealed class HranaValue
{
    /// <summary>Type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>Value.</summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    /// <summary>Base64.</summary>
    [JsonPropertyName("base64")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Base64 { get; set; }
}

/// <summary>
/// Represents a Hrana protocol batch response.
/// </summary>
internal sealed class HranaBatchResponse
{
    /// <summary>Baton.</summary>
    [JsonPropertyName("baton")]
    public string? Baton { get; set; }

    /// <summary>BaseUrl.</summary>
    [JsonPropertyName("base_url")]
    public string? BaseUrl { get; set; }

    /// <summary>Results.</summary>
    [JsonPropertyName("results")]
    public List<HranaResult> Results { get; set; } = new();
}

/// <summary>
/// Represents a single result in a Hrana batch response.
/// </summary>
internal sealed class HranaResult
{
    /// <summary>Type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>Response.</summary>
    [JsonPropertyName("response")]
    public HranaResponse? Response { get; set; }

    /// <summary>Error.</summary>
    [JsonPropertyName("error")]
    public HranaError? Error { get; set; }
}

/// <summary>
/// Represents the response portion of a Hrana result.
/// </summary>
internal sealed class HranaResponse
{
    /// <summary>Type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>Result.</summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaQueryResult? Result { get; set; }

    /// <summary>BatchResult.</summary>
    [JsonPropertyName("batch_result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatchResult? BatchResult { get; set; }

    // Error fields
    /// <summary>Error.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaError? Error { get; set; }
}

/// <summary>
/// Represents query result data in Hrana protocol.
/// </summary>
internal sealed class HranaQueryResult
{
    /// <summary>Cols.</summary>
    [JsonPropertyName("cols")]
    public List<HranaColumn>? Cols { get; set; }

    /// <summary>Rows.</summary>
    [JsonPropertyName("rows")]
    public List<List<HranaValue>>? Rows { get; set; }

    /// <summary>AffectedRowCount.</summary>
    [JsonPropertyName("affected_row_count")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong AffectedRowCount { get; set; }

    /// <summary>LastInsertRowid.</summary>
    [JsonPropertyName("last_insert_rowid")]
    public string? LastInsertRowid { get; set; }

    /// <summary>ReplicationIndex.</summary>
    [JsonPropertyName("replication_index")]
    public string? ReplicationIndex { get; set; }

    /// <summary>RowsRead.</summary>
    [JsonPropertyName("rows_read")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong RowsRead { get; set; }

    /// <summary>RowsWritten.</summary>
    [JsonPropertyName("rows_written")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong RowsWritten { get; set; }

    /// <summary>QueryDurationMs.</summary>
    [JsonPropertyName("query_duration_ms")]
    public double QueryDurationMs { get; set; }
}

/// <summary>
/// Represents a column definition in Hrana protocol.
/// </summary>
internal sealed class HranaColumn
{
    /// <summary>Name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>DeclType.</summary>
    [JsonPropertyName("decltype")]
    public string? DeclType { get; set; }
}

/// <summary>
/// Represents an error in Hrana protocol.
/// </summary>
internal sealed class HranaError
{
    /// <summary>Message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>
/// Constants for Hrana protocol types.
/// </summary>
internal static class HranaTypes
{
    // Request types
    /// <summary>string.</summary>
    public const string Execute = "execute";
    /// <summary>string.</summary>
    public const string Close = "close";
    /// <summary>string.</summary>
    public const string Batch = "batch";
    /// <summary>string.</summary>
    public const string Sequence = "sequence";

    // Response types
    /// <summary>string.</summary>
    public const string Ok = "ok";
    /// <summary>string.</summary>
    public const string Error = "error";

    // Value types
    /// <summary>string.</summary>
    public const string Null = "null";
    /// <summary>string.</summary>
    public const string Integer = "integer";
    /// <summary>string.</summary>
    public const string Float = "float";
    /// <summary>string.</summary>
    public const string Text = "text";
    /// <summary>string.</summary>
    public const string Blob = "blob";
}

/// <summary>
/// Represents a batch of statements for conditional execution.
/// </summary>
internal sealed class HranaBatch
{
    /// <summary>Steps.</summary>
    [JsonPropertyName("steps")]
    public List<HranaBatchStep> Steps { get; set; } = new();
}

/// <summary>
/// Represents a single step in a batch.
/// </summary>
internal sealed class HranaBatchStep
{
    /// <summary>Statement.</summary>
    [JsonPropertyName("stmt")]
    public HranaStatement Statement { get; set; }

    /// <summary>Condition.</summary>
    [JsonPropertyName("condition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatchCondition? Condition { get; set; }
}

/// <summary>
/// Represents a condition for batch step execution.
/// </summary>
internal sealed class HranaBatchCondition
{
    /// <summary>Type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>Step.</summary>
    [JsonPropertyName("step")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Step { get; set; }

    /// <summary>InnerCondition.</summary>
    [JsonPropertyName("cond")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HranaBatchCondition? InnerCondition { get; set; }
}

/// <summary>
/// Represents a batch result from the server.
/// </summary>
internal sealed class HranaBatchResult
{
    /// <summary>StepResults.</summary>
    [JsonPropertyName("step_results")]
    public List<HranaQueryResult?> StepResults { get; set; } = new();

    /// <summary>StepErrors.</summary>
    [JsonPropertyName("step_errors")]
    public List<HranaError?> StepErrors { get; set; } = new();
}
