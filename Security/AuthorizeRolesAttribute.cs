using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace StudioGirlPink.Security;

/// <summary>
/// Filtro de autorización declarativa RBAC multi-rol ('ADMIN', 'CLIENTE').
/// Aísla el contexto de ejecución y bloquea escalado horizontal o vertical de privilegios.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRolesAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _roles;

    public AuthorizeRolesAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            if (IsApiRequest(context.HttpContext))
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    error = "Unauthorized",
                    message = "Autenticación requerida. Token JWT no proporcionado o inválido."
                }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
            else
            {
                context.Result = new RedirectToActionResult("AdminLogin", "Account", null);
            }
            return;
        }

        // Verificar coincidencia de rol
        var hasRole = _roles.Any(r => 
            user.IsInRole(r) ||
            (r.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) && user.IsInRole("Administrator")) ||
            (r.Equals("CLIENTE", StringComparison.OrdinalIgnoreCase) && user.IsInRole("Client"))
        );

        if (!hasRole)
        {
            if (IsApiRequest(context.HttpContext))
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    error = "Forbidden",
                    message = $"Acceso denegado: se requiere uno de los siguientes roles: {string.Join(", ", _roles)}."
                }) { StatusCode = StatusCodes.Status403Forbidden };
            }
            else
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }
            return;
        }

        await next();
    }

    private static bool IsApiRequest(HttpContext httpContext)
    {
        return httpContext.Request.Path.StartsWithSegments("/api") ||
               httpContext.Request.Path.StartsWithSegments("/auth") ||
               httpContext.Request.Headers.Accept.Any(a => a?.Contains("application/json") == true);
    }
}
