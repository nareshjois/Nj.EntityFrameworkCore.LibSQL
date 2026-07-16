using System.Runtime.InteropServices;

namespace Nj.LibSql.Bindings;

/// <summary>
/// Helper routines for working with libsql native results and error pointers.
/// </summary>
internal static class LibSqlHelper
{
    /// <summary>
    /// Gets a string from a native pointer, optionally freeing it with <c>libsql_free_string</c>.
    /// </summary>
    internal static string? GetStringFromPtr(IntPtr ptr, bool shouldFree = false)
    {
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            if (shouldFree)
            {
                LibSqlNative.libsql_free_string(ptr);
            }
        }
    }

    /// <summary>
    /// Checks whether a libsql operation result indicates success.
    /// </summary>
    internal static bool IsSuccess(int result)
        => result == 0;

    /// <summary>
    /// Gets an error message from a native error message pointer.
    /// </summary>
    internal static string GetErrorMessage(IntPtr errorPtr)
    {
        if (errorPtr == IntPtr.Zero)
        {
            return "Unknown error";
        }

        return Marshal.PtrToStringUTF8(errorPtr) ?? "Unknown error";
    }

    /// <summary>
    /// Throws an exception if the libsql operation failed.
    /// </summary>
    internal static void ThrowIfError(int result, string? errorMessage = null)
    {
        if (!IsSuccess(result))
        {
            throw new InvalidOperationException(errorMessage ?? $"libsql operation failed with code: {result}");
        }
    }
}
