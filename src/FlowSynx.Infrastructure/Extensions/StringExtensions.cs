using System.Security.Cryptography;
using System.Text;

namespace FlowSynx.Infrastructure.Extensions;

internal static class StringExtensions
{
    public static string Md5HashKey(this string? key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            using var hasher = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(key);
            var hashBytes = hasher.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}