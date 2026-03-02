using System.Security.Cryptography;

namespace LinkShorter.Services;

public class UrlShortenerService
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    // Base62
    public string GenerateCode(int length = 6)
    {
        return new string(Enumerable.Repeat(Chars, length)
            .Select(s => s[RandomNumberGenerator.GetInt32(Chars.Length)])
            .ToArray());
    }
}