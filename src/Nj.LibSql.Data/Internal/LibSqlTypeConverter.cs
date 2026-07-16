using System.Globalization;
using System.Text;

namespace Nj.LibSql.Data.Internal;

/// <summary>
/// Provides conversion utilities between .NET types and libSQL values.
/// </summary>
internal static class LibSqlTypeConverter
{
    // Match Microsoft.Data.Sqlite / EF Core Sqlite: up to 7 fractional digits (100ns ticks).
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";
    private const string DateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz";
    private const string DateOnlyFormat = "yyyy-MM-dd";

    private static readonly string[] DateTimeFormats =
    [
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "MM/dd/yyyy HH:mm:ss.FFFFFFF",
        "MM/dd/yyyy HH:mm:ss.fff",
        "MM/dd/yyyy HH:mm:ss",
        "MM/dd/yyyy"
    ];

    private static readonly string[] DateTimeOffsetFormats =
    [
        "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-dd HH:mm:ss.fffzzz",
        "yyyy-MM-dd HH:mm:sszzz",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-ddTHH:mm:ss.fffzzz",
        "yyyy-MM-ddTHH:mm:sszzz",
        "MM/dd/yyyy HH:mm:ss.FFFFFFFzzz",
        "MM/dd/yyyy HH:mm:ss.fffzzz",
        "MM/dd/yyyy HH:mm:ss zzz",
        "MM/dd/yyyy HH:mm:sszzz",
        "MM/dd/yyyy HH:mm:ss"
    ];

    private static readonly string[] TimeOnlyFormats =
    [
        "HH:mm:ss.FFFFFFF",
        "HH:mm:ss.fff",
        "HH:mm:ss"
    ];

    public static object ConvertToLibSql(object? value, LibSqlDbType targetType)
    {
        if (value is null || value == DBNull.Value)
        {
            return DBNull.Value;
        }

        return targetType switch
        {
            LibSqlDbType.Integer => ConvertToInteger(value),
            LibSqlDbType.Real => ConvertToReal(value),
            LibSqlDbType.Text => ConvertToText(value),
            LibSqlDbType.Blob => ConvertToBlob(value),
            LibSqlDbType.Null => DBNull.Value,
            _ => throw new ArgumentException($"Unsupported libSQL type: {targetType}")
        };
    }

    public static object ConvertFromLibSql(object? value, Type targetType)
    {
        if (value is null || value == DBNull.Value)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                throw new InvalidOperationException($"Cannot convert NULL to non-nullable type {targetType.Name}");
            }

            return null!;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            targetType = underlyingType;
        }

        return targetType switch
        {
            _ when targetType == typeof(bool) => ConvertToBoolean(value),
            _ when targetType == typeof(byte) => ConvertToByte(value),
            _ when targetType == typeof(sbyte) => ConvertToSByte(value),
            _ when targetType == typeof(short) => ConvertToInt16(value),
            _ when targetType == typeof(ushort) => ConvertToUInt16(value),
            _ when targetType == typeof(int) => ConvertToInt32(value),
            _ when targetType == typeof(uint) => ConvertToUInt32(value),
            _ when targetType == typeof(long) => ConvertToInt64(value),
            _ when targetType == typeof(ulong) => ConvertToUInt64(value),
            _ when targetType == typeof(float) => ConvertToSingle(value),
            _ when targetType == typeof(double) => ConvertToDouble(value),
            _ when targetType == typeof(decimal) => ConvertToDecimal(value),
            _ when targetType == typeof(string) => ConvertToString(value),
            _ when targetType == typeof(char) => ConvertToChar(value),
            _ when targetType == typeof(Guid) => ConvertToGuid(value),
            _ when targetType == typeof(DateTime) => ConvertToDateTime(value),
            _ when targetType == typeof(DateTimeOffset) => ConvertToDateTimeOffset(value),
            _ when targetType == typeof(TimeSpan) => ConvertToTimeSpan(value),
            _ when targetType == typeof(DateOnly) => ConvertToDateOnly(value),
            _ when targetType == typeof(TimeOnly) => ConvertToTimeOnly(value),
            _ when targetType == typeof(byte[]) => ConvertToByteArray(value),
            _ => Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture)
        };
    }

    private static long ConvertToInteger(object value)
        => value switch
        {
            bool b => b ? 1L : 0L,
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            uint ui => ui,
            long l => l,
            ulong ul => checked((long)ul),
            float f => checked((long)f),
            double d => checked((long)d),
            decimal dec => checked((long)dec),
            string str => long.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt64(value, CultureInfo.InvariantCulture)
        };

    private static double ConvertToReal(object value)
        => value switch
        {
            float f => f,
            double d => d,
            decimal dec => (double)dec,
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            uint ui => ui,
            long l => l,
            ulong ul => ul,
            string str => double.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };

    private static string ConvertToText(object value)
        => value switch
        {
            string str => str,
            char c => c.ToString(),
            Guid g => g.ToString(),
            DateTime dt => dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString(DateTimeOffsetFormat, CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString(DateOnlyFormat, CultureInfo.InvariantCulture),
            TimeOnly t => t.Ticks % TimeSpan.TicksPerSecond == 0
                ? t.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                : t.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

    private static byte[] ConvertToBlob(object value)
        => value switch
        {
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to BLOB")
        };

    private static bool ConvertToBoolean(object value)
        => value switch
        {
            bool b => b,
            long l => l != 0,
            double d => d != 0.0,
            string str => bool.Parse(str),
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
        };

    private static byte ConvertToByte(object value)
        => value switch
        {
            long l => checked((byte)l),
            double d => checked((byte)d),
            string str => byte.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToByte(value, CultureInfo.InvariantCulture)
        };

    private static sbyte ConvertToSByte(object value)
        => value switch
        {
            long l => checked((sbyte)l),
            double d => checked((sbyte)d),
            string str => sbyte.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToSByte(value, CultureInfo.InvariantCulture)
        };

    private static short ConvertToInt16(object value)
        => value switch
        {
            long l => checked((short)l),
            double d => checked((short)d),
            string str => short.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt16(value, CultureInfo.InvariantCulture)
        };

    private static ushort ConvertToUInt16(object value)
        => value switch
        {
            long l => checked((ushort)l),
            double d => checked((ushort)d),
            string str => ushort.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToUInt16(value, CultureInfo.InvariantCulture)
        };

    private static int ConvertToInt32(object value)
        => value switch
        {
            long l => checked((int)l),
            double d => checked((int)d),
            string str => int.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
        };

    private static uint ConvertToUInt32(object value)
        => value switch
        {
            long l => checked((uint)l),
            double d => checked((uint)d),
            string str => uint.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToUInt32(value, CultureInfo.InvariantCulture)
        };

    private static long ConvertToInt64(object value)
        => value switch
        {
            long l => l,
            double d => checked((long)d),
            string str => long.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToInt64(value, CultureInfo.InvariantCulture)
        };

    private static ulong ConvertToUInt64(object value)
        => value switch
        {
            long l => checked((ulong)l),
            double d => checked((ulong)d),
            string str => ulong.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToUInt64(value, CultureInfo.InvariantCulture)
        };

    private static float ConvertToSingle(object value)
        => value switch
        {
            double d => checked((float)d),
            long l => l,
            string str => float.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToSingle(value, CultureInfo.InvariantCulture)
        };

    private static double ConvertToDouble(object value)
        => value switch
        {
            double d => d,
            long l => l,
            string str => double.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };

    private static decimal ConvertToDecimal(object value)
        => value switch
        {
            double d => (decimal)d,
            long l => l,
            string str => decimal.Parse(str, CultureInfo.InvariantCulture),
            _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
        };

    private static string ConvertToString(object value)
        => value switch
        {
            string str => str,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => value.ToString() ?? string.Empty
        };

    private static char ConvertToChar(object value)
        => value switch
        {
            string str when str.Length == 1 => str[0],
            long l when l >= 0 && l <= char.MaxValue => (char)l,
            _ => Convert.ToChar(value, CultureInfo.InvariantCulture)
        };

    private static Guid ConvertToGuid(object value)
        => value switch
        {
            string str => Guid.Parse(str),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            Guid g => g,
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Guid.")
        };

    private static DateTime ConvertToDateTime(object value)
        => value switch
        {
            string str => DateTime.ParseExact(
                str,
                DateTimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal),
            long ticks => new DateTime(ticks),
            DateTime dt => dt,
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
        };

    private static DateTimeOffset ConvertToDateTimeOffset(object value)
        => value switch
        {
            string str => DateTimeOffset.ParseExact(
                str,
                DateTimeOffsetFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces),
            long ticks => new DateTimeOffset(ticks, TimeSpan.Zero),
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateTimeOffset.")
        };

    private static TimeSpan ConvertToTimeSpan(object value)
        => value switch
        {
            string str => TimeSpan.Parse(str, CultureInfo.InvariantCulture),
            long ticks => new TimeSpan(ticks),
            TimeSpan ts => ts,
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to TimeSpan.")
        };

    private static DateOnly ConvertToDateOnly(object value)
        => value switch
        {
            string str => DateOnly.ParseExact(str, DateOnlyFormat, CultureInfo.InvariantCulture),
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to DateOnly.")
        };

    private static TimeOnly ConvertToTimeOnly(object value)
        => value switch
        {
            string str => TimeOnly.ParseExact(str, TimeOnlyFormats, CultureInfo.InvariantCulture),
            TimeOnly t => t,
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to TimeOnly.")
        };

    private static byte[] ConvertToByteArray(object value)
        => value switch
        {
            byte[] bytes => bytes,
            string str => Encoding.UTF8.GetBytes(str),
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to byte array")
        };

    public static bool IsNull(object? value)
        => value is null || value == DBNull.Value;
}
