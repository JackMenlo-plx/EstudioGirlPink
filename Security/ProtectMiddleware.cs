using System.Security.Claims;
using StudioGirlPink.Services.Auth;

namespace StudioGirlPink.Security;

/// <summary>
/// Middleware "Protect" para autenticación desacoplada vía JWT extraído de Cookies HTTP-Only o Headers Authorization.
/// Inyecta el ClaimsPrincipal y aísla el contexto del usuario para prevenir colisiones o escalado de privilegios.
/// </summary>
public class ProtectMiddleware(RequestDelegate next, ILogger<ProtectMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        string? token = null;

        // 1. Extraer JWT de Cookie segura HTTP-Only
        if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        {
            token = cookieToken;
        }
        // 2. Fallback a Header Authorization Bearer
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = headerValue["Bearer ".Length..].Trim();
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            var principal = jwtService.ValidateToken(token);
            if (principal != null)
            {
                context.User = principal;
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                context.Items["userId"] = userId;
                context.Items["req.user.id"] = userId;
            }
            else
            {
                logger.LogDebug("Token JWT inválido o expirado recibido en ProtectMiddleware.");
            }
        }

        await next(context);
    }
}
