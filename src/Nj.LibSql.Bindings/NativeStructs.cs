using System.Runtime.InteropServices;

namespace Nj.LibSql.Bindings;

/// <summary>
/// libsql result codes (based on SQLite result codes).
/// </summary>
internal enum LibSqlResultCode
{
    Ok = 0,
    Error = 1,
    Internal = 2,
    Perm = 3,
    Abort = 4,
    Busy = 5,
    Locked = 6,
    NoMem = 7,
    ReadOnly = 8,
    Interrupt = 9,
    IoErr = 10,
    Corrupt = 11,
    NotFound = 12,
    Full = 13,
    CantOpen = 14,
    Protocol = 15,
    Empty = 16,
    Schema = 17,
    TooBig = 18,
    Constraint = 19,
    Mismatch = 20,
    Misuse = 21,
    NoLfs = 22,
    Auth = 23,
    Format = 24,
    Range = 25,
    NotADb = 26,
    Notice = 27,
    Warning = 28,
    Row = 100,
    Done = 101
}

/// <summary>
/// libsql data type constants.
/// </summary>
internal static class LibSqlType
{
    public const int Int = 1;
    public const int Float = 2;
    public const int Text = 3;
    public const int Blob = 4;
    public const int Null = 5;
}

/// <summary>
/// <c>libsql_config</c> structure for database configuration.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LibSqlConfig
{
    public IntPtr DbPath;
    public IntPtr PrimaryUrl;
    public IntPtr AuthToken;
    public byte ReadYourWrites;
    public IntPtr EncryptionKey;
    public int SyncInterval;
    public byte WithWebpki;
    public byte Offline;
}

/// <summary>
/// Replication statistics returned from a sync operation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LibSqlReplicated
{
    public int FrameNo;
    public int FramesSynced;
}

/// <summary>
/// A blob buffer returned from libsql.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LibSqlBlob
{
    public IntPtr Ptr;
    public int Len;

    public readonly byte[] ToByteArray()
    {
        if (Ptr == IntPtr.Zero || Len <= 0)
        {
            return [];
        }

        var result = new byte[Len];
        Marshal.Copy(Ptr, result, 0, Len);
        return result;
    }
}

/// <summary>
/// Transaction behavior modes.
/// </summary>
internal enum LibSqlTransactionBehavior
{
    Deferred = 0,
    Immediate = 1,
    Exclusive = 2
}
