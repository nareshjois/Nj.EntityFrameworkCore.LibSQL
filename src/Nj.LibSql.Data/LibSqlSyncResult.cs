namespace Nj.LibSql.Data;

/// <summary>
/// Result of an embedded-replica <see cref="LibSqlConnection.Sync"/> operation
/// (native <c>libsql_sync2</c> frames).
/// </summary>
public readonly struct LibSqlSyncResult : IEquatable<LibSqlSyncResult>
{
    /// <summary>Creates a sync result.</summary>
    public LibSqlSyncResult(int frameNo, int framesSynced)
    {
        FrameNo = frameNo;
        FramesSynced = framesSynced;
    }

    /// <summary>Highest frame number after sync.</summary>
    public int FrameNo { get; }

    /// <summary>Number of frames applied in this sync call.</summary>
    public int FramesSynced { get; }

    /// <summary>Equals(LibSqlSyncResult.</summary>
    public bool Equals(LibSqlSyncResult other)
        => FrameNo == other.FrameNo && FramesSynced == other.FramesSynced;

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is LibSqlSyncResult other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(FrameNo, FramesSynced);

    /// <inheritdoc />
    public override string ToString()
        => $"FrameNo={FrameNo}, FramesSynced={FramesSynced}";

    /// <summary>operator.</summary>
    public static bool operator ==(LibSqlSyncResult left, LibSqlSyncResult right)
        => left.Equals(right);

    /// <summary>operator.</summary>
    public static bool operator !=(LibSqlSyncResult left, LibSqlSyncResult right)
        => !left.Equals(right);
}
