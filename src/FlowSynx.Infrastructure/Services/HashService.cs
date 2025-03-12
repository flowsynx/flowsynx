using FlowSynx.Application.Services;
using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.Services;

public class HashService : IHashService
{
    public string Hash(string input)
    {
        try
        {
            using var hasher = MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hashBytes = hasher.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}