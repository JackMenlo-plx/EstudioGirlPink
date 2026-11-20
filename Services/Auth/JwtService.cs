using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace StudioGirlPink.Services.Auth;

public interface IJwtService
{
    string GenerateToken(IdentityUser user, string role, TimeSpan? lifetime = null);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtService(IConfiguration configuration, ILogger<JwtService> logger) : IJwtService
{
    private string Secret => configuration["JWT_SECRET"] ?? "StudioGirlPink_Enterprise_JWT_Secret_SuperSecure_2026_Minimum32Bytes!";

    public string GenerateToken(IdentityUser user, string role, TimeSpan? lifetime = null)
    {
        var expiration = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromHours(8)).ToUnixTimeSeconds();
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new Dictionary<string, object>
        {
            { "sub", user.Id },
            { "email", user.Email ?? "" },
            { "role", role },
            { "iat", issuedAt },
            { "exp", expiration },
            { "jti", Guid.NewGuid().ToString("N") }
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = ComputeHmacSha256(unsignedToken, Secret);
        var encodedSignature = Base64UrlEncode(signature);

        return $"{unsignedToken}.{encodedSignature}";
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        var expectedSignature = Base64UrlEncode(ComputeHmacSha256(unsignedToken, Secret));

        // Comparación segura en tiempo constante contra ataques de timing
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(parts[2]), Encoding.UTF8.GetBytes(expectedSignature)))
        {
            logger.LogWarning("Firma de JWT inválida.");
            return null;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("exp", out var expProp))
            {
                var exp = expProp.GetInt64();
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                {
                    logger.LogInformation("JWT expirado.");
                    return null;
                }
            }

            var claims = new List<Claim>();
            if (root.TryGetProperty("sub", out var subProp))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, subProp.GetString() ?? ""));
            }
            if (root.TryGetProperty("email", out var emailProp))
            {
                claims.Add(new Claim(ClaimTypes.Email, emailProp.GetString() ?? ""));
                claims.Add(new Claim(ClaimTypes.Name, emailProp.GetString() ?? ""));
            }
            if (root.TryGetProperty("role", out var roleProp))
            {
                var role = roleProp.GetString() ?? "";
                claims.Add(new Claim(ClaimTypes.Role, role));
                // Soportar aliases compatibles
                if (role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                }
                else if (role.Equals("CLIENTE", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Client"));
                }
            }

            var identity = new ClaimsIdentity(claims, "CustomJwt");
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al decodificar JWT payload.");
            return null;
        }
    }

    private static byte[] ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }
}
