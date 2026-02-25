using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Orchestrator.Services;

public class SignatureVerifier(IOptions<GithubWebhookOptions> options)
{
    private readonly GithubWebhookOptions _options = options.Value;

    public bool IsValid(string? signatureHeader, string payload)
    {
        if (string.IsNullOrWhiteSpace(_options.Secret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256="))
        {
            return false;
        }

        var expected = signatureHeader[7..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(computed));
    }
}
