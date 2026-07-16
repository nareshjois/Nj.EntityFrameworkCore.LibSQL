using System.Data;

namespace Nj.LibSql.Data.Internal;

/// <summary>
/// Provides type mapping between .NET types and libSQL types.
/// </summary>
internal static class LibSqlTypeMapper
{
    private static readonly Dictionary<Type, LibSqlDbType> NetTypeToLibSqlType = new()
    {
        { typeof(bool), LibSqlDbType.Integer },
        { typeof(byte), LibSqlDbType.Integer },
        { typeof(sbyte), LibSqlDbType.Integer },
        { typeof(short), LibSqlDbType.Integer },
        { typeof(ushort), LibSqlDbType.Integer },
        { typeof(int), LibSqlDbType.Integer },
        { typeof(uint), LibSqlDbType.Integer },
        { typeof(long), LibSqlDbType.Integer },
        { typeof(ulong), LibSqlDbType.Integer },
        { typeof(float), LibSqlDbType.Real },
        { typeof(double), LibSqlDbType.Real },
        { typeof(decimal), LibSqlDbType.Real },
        { typeof(string), LibSqlDbType.Text },
        { typeof(char), LibSqlDbType.Text },
        { typeof(Guid), LibSqlDbType.Text },
        { typeof(DateTime), LibSqlDbType.Text },
        { typeof(DateTimeOffset), LibSqlDbType.Text },
        { typeof(TimeSpan), LibSqlDbType.Text },
        { typeof(DateOnly), LibSqlDbType.Text },
        { typeof(TimeOnly), LibSqlDbType.Text },
        { typeof(byte[]), LibSqlDbType.Blob },
        { typeof(bool?), LibSqlDbType.Integer },
        { typeof(byte?), LibSqlDbType.Integer },
        { typeof(sbyte?), LibSqlDbType.Integer },
        { typeof(short?), LibSqlDbType.Integer },
        { typeof(ushort?), LibSqlDbType.Integer },
        { typeof(int?), LibSqlDbType.Integer },
        { typeof(uint?), LibSqlDbType.Integer },
        { typeof(long?), LibSqlDbType.Integer },
        { typeof(ulong?), LibSqlDbType.Integer },
        { typeof(float?), LibSqlDbType.Real },
        { typeof(double?), LibSqlDbType.Real },
        { typeof(decimal?), LibSqlDbType.Real },
        { typeof(char?), LibSqlDbType.Text },
        { typeof(Guid?), LibSqlDbType.Text },
        { typeof(DateTime?), LibSqlDbType.Text },
        { typeof(DateTimeOffset?), LibSqlDbType.Text },
        { typeof(TimeSpan?), LibSqlDbType.Text },
        { typeof(DateOnly?), LibSqlDbType.Text },
        { typeof(TimeOnly?), LibSqlDbType.Text },
    };

    private static readonly Dictionary<DbType, LibSqlDbType> DbTypeToLibSqlType = new()
    {
        { DbType.Boolean, LibSqlDbType.Integer },
        { DbType.Byte, LibSqlDbType.Integer },
        { DbType.SByte, LibSqlDbType.Integer },
        { DbType.Int16, LibSqlDbType.Integer },
        { DbType.UInt16, LibSqlDbType.Integer },
        { DbType.Int32, LibSqlDbType.Integer },
        { DbType.UInt32, LibSqlDbType.Integer },
        { DbType.Int64, LibSqlDbType.Integer },
        { DbType.UInt64, LibSqlDbType.Integer },
        { DbType.Single, LibSqlDbType.Real },
        { DbType.Double, LibSqlDbType.Real },
        { DbType.Decimal, LibSqlDbType.Real },
        { DbType.Currency, LibSqlDbType.Real },
        { DbType.VarNumeric, LibSqlDbType.Real },
        { DbType.String, LibSqlDbType.Text },
        { DbType.StringFixedLength, LibSqlDbType.Text },
        { DbType.AnsiString, LibSqlDbType.Text },
        { DbType.AnsiStringFixedLength, LibSqlDbType.Text },
        { DbType.Guid, LibSqlDbType.Text },
        { DbType.Xml, LibSqlDbType.Text },
        { DbType.DateTime, LibSqlDbType.Text },
        { DbType.DateTime2, LibSqlDbType.Text },
        { DbType.DateTimeOffset, LibSqlDbType.Text },
        { DbType.Date, LibSqlDbType.Text },
        { DbType.Time, LibSqlDbType.Text },
        { DbType.Binary, LibSqlDbType.Blob },
        { DbType.Object, LibSqlDbType.Blob }
    };

    public static LibSqlDbType GetLibSqlType(Type? type)
    {
        if (type == null)
        {
            return LibSqlDbType.Null;
        }

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        return NetTypeToLibSqlType.TryGetValue(type, out var libSqlType) ? libSqlType : LibSqlDbType.Text;
    }

    public static LibSqlDbType GetLibSqlType(DbType dbType)
        => DbTypeToLibSqlType.TryGetValue(dbType, out var libSqlType) ? libSqlType : LibSqlDbType.Text;

    public static DbType GetDbType(Type? type)
    {
        if (type == null)
        {
            return DbType.Object;
        }

        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        return type switch
        {
            _ when type == typeof(bool) => DbType.Boolean,
            _ when type == typeof(byte) => DbType.Byte,
            _ when type == typeof(sbyte) => DbType.SByte,
            _ when type == typeof(short) => DbType.Int16,
            _ when type == typeof(ushort) => DbType.UInt16,
            _ when type == typeof(int) => DbType.Int32,
            _ when type == typeof(uint) => DbType.UInt32,
            _ when type == typeof(long) => DbType.Int64,
            _ when type == typeof(ulong) => DbType.UInt64,
            _ when type == typeof(float) => DbType.Single,
            _ when type == typeof(double) => DbType.Double,
            _ when type == typeof(decimal) => DbType.Decimal,
            _ when type == typeof(string) => DbType.String,
            _ when type == typeof(char) => DbType.StringFixedLength,
            _ when type == typeof(Guid) => DbType.Guid,
            _ when type == typeof(DateTime) => DbType.DateTime,
            _ when type == typeof(DateTimeOffset) => DbType.DateTimeOffset,
            _ when type == typeof(TimeSpan) => DbType.Time,
            _ when type == typeof(DateOnly) => DbType.Date,
            _ when type == typeof(TimeOnly) => DbType.Time,
            _ when type == typeof(byte[]) => DbType.Binary,
            _ => DbType.Object
        };
    }
}
