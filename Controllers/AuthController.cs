using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudioGirlPink.Security;
using StudioGirlPink.Services.Auth;

namespace StudioGirlPink.Controllers;

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record LoginRequest(string Email, string Password);

[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IPasswordResetService passwordResetService,
    IJwtService jwtService,
    IHostEnvironment environment,
    ILogger<AuthController> logger) : ControllerBase
{
    // =========================================================================
    // 1. SOLICITUD DE RECUPERACIÓN DE CONTRASEÑA (POST /auth/forgot-password)
    // Rate limit estricto: 5 intentos / 15 min
    // =========================================================================
    [HttpPost("forgot-password")]
    [EnableRateLimiting("strict-auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { success = false, message = "El campo correo electrónico es obligatorio." });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await passwordResetService.InitiatePasswordResetAsync(request.Email, baseUrl, cancellationToken);

        // Respuesta genérica para mitigar User Enumeration
        return Ok(new
        {
            success = true,
            message = "Si el correo está registrado en Studio Girl Pink, recibirás un enlace de recuperación con vigencia de 20 minutos."
        });
    }

    // =========================================================================
    // 2. CONSUMO DE TOKEN Y RESTABLECIMIENTO (POST /auth/reset-password)
    // Redirección para navegadores web (GET /auth/reset-password)
    // =========================================================================
    [HttpGet("reset-password")]
    public IActionResult ResetPasswordBrowser([FromQuery] string? token, [FromQuery] string? email)
    {
        return RedirectToAction("ResetPassword", "Account", new { token, email });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("strict-auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { success = false, message = "Todos los campos (email, token, newPassword) son requeridos." });
        }

        var (success, message) = await passwordResetService.ResetPasswordAsync(
            request.Email, 
            request.Token, 
            request.NewPassword, 
            cancellationToken);

        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new
        {
            success = true,
            message = "Contraseña actualizada exitosamente. Todas las sesiones activas han sido invalidadas."
        });
    }

    // =========================================================================
    // 3. INICIO DE SESIÓN MULTIUSUARIO JWT (POST /auth/login)
    // Soportando roles: ADMIN y CLIENTE con Cookie HTTP-Only
    // =========================================================================
    [HttpPost("login")]
    [EnableRateLimiting("strict-auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Email y contraseña son requeridos." });
        }

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(cleanEmail);
        if (user == null)
        {
            return Unauthorized(new { success = false, message = "Credenciales incorrectas." });
        }

        var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (passwordCheck.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                success = false,
                message = "Cuenta bloqueada temporalmente por exceso de intentos fallidos. Intenta más tarde."
            });
        }

        if (!passwordCheck.Succeeded)
        {
            return Unauthorized(new { success = false, message = "Credenciales incorrectas." });
        }

        // Obtener rol del usuario (ADMIN o CLIENTE)
        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "CLIENTE";
        if (primaryRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase)) primaryRole = "ADMIN";
        if (primaryRole.Equals("Client", StringComparison.OrdinalIgnoreCase)) primaryRole = "CLIENTE";

        var token = jwtService.GenerateToken(user, primaryRole);

        // Inyectar JWT en Cookie HTTP-Only
        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        logger.LogInformation("Inicio de sesión exitoso para usuario {Email} con rol {Role}.", user.Email, primaryRole);

        return Ok(new
        {
            success = true,
            message = "Autenticación exitosa.",
            token, // También retornado en payload para clientes móviles o SPAs
            user = new
            {
                id = user.Id,
                email = user.Email,
                role = primaryRole
            }
        });
    }

    // =========================================================================
    // 4. CIERRE DE SESIÓN (POST /auth/logout)
    // =========================================================================
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        Response.Cookies.Delete("access_token");
        return Ok(new { success = true, message = "Sesión cerrada exitosamente." });
    }

    // =========================================================================
    // 5. CONTEXTO DE USUARIO PROTEGIDO (GET /auth/me)
    // =========================================================================
    [HttpGet("me")]
    [AuthorizeRoles("ADMIN", "CLIENTE")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            success = true,
            user = new
            {
                id = userId,
                email,
                role
            }
        });
    }
}
