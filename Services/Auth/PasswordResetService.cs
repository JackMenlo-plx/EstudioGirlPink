using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudioGirlPink.Data;
using StudioGirlPink.Models;
using StudioGirlPink.Services.Email;

namespace StudioGirlPink.Services.Auth;

public interface IPasswordResetService
{
    Task<bool> InitiatePasswordResetAsync(string email, string? baseUrl = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ResetPasswordAsync(string email, string plainToken, string newPassword, CancellationToken cancellationToken = default);
}

public class PasswordResetService(
    AppDbContext db,
    UserManager<IdentityUser> userManager,
    IEmailProvider emailProvider,
    IConfiguration configuration,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    public const int TokenExpirationMinutes = 20;

    public async Task<bool> InitiatePasswordResetAsync(string email, string? baseUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var cleanEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(cleanEmail);

        // Mitigación de enumeración de usuarios: devolvemos siempre true al cliente
        if (user == null)
        {
            logger.LogInformation("Solicitud de recuperación para correo no registrado: {Email}", cleanEmail);
            return true;
        }

        // 1. Generación de token criptográficamente seguro de 32 bytes
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var plainToken = Convert.ToHexString(randomBytes).ToLowerInvariant();

        // 2. Hash SHA-256 del token para almacenamiento seguro
        var tokenHash = ComputeSha256Hash(plainToken);

        // 3. Invalidar tokens previos no consumidos del mismo usuario
        var oldTokens = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync(cancellationToken);

        db.PasswordResetTokens.RemoveRange(oldTokens);

        // 4. Guardar nuevo token con expiración de 20 minutos
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync(cancellationToken);

        // 5. Construir enlace con token en texto plano hacia la vista de usuario
        var frontendUrl = configuration["FRONTEND_URL"] ?? baseUrl ?? "http://localhost:5000";
        var resetLink = $"{frontendUrl.TrimEnd('/')}/cuenta/restablecer-clave?token={plainToken}&email={Uri.EscapeDataString(cleanEmail)}";

        // 6. Enviar correo vía servicio desacoplado
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 580px; margin: 0 auto; padding: 25px; border: 1px solid #fbcfe8; border-radius: 14px; background: #fffdfd;'>
                <h2 style='color: #db2777; margin-top: 0;'>🌸 Studio Girl Pink</h2>
                <h3 style='color: #831843;'>Restablecimiento de Contraseña</h3>
                <p style='color: #475569; font-size: 15px;'>Hola, recibimos una solicitud para restablecer la contraseña de tu cuenta (Cliente / Administrador).</p>
                <div style='text-align: center; margin: 25px 0;'>
                    <a href='{resetLink}' style='background: #db2777; color: #ffffff; padding: 12px 24px; text-decoration: none; font-weight: bold; border-radius: 8px; display: inline-block;'>
                        Restablecer mi Contraseña
                    </a>
                </div>
                <p style='color: #64748b; font-size: 13px;'>Este enlace expirará en <strong>{TokenExpirationMinutes} minutos</strong> por tu seguridad.</p>
                <p style='color: #94a3b8; font-size: 12px; margin-top: 20px; border-top: 1px solid #f1f5f9; padding-top: 10px;'>
                    Si tú no realizaste esta solicitud, puedes ignorar este mensaje; tu contraseña actual continuará siendo segura.
                </p>
            </div>";

        await emailProvider.SendEmailAsync(new EmailPayload(
            To: cleanEmail,
            Subject: "Recupera tu contraseña - Studio Girl Pink",
            HtmlBody: htmlBody
        ), cancellationToken);

        return true;
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string plainToken, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(plainToken) || string.IsNullOrWhiteSpace(newPassword))
        {
            return (false, "Datos incompletos para restablecer la contraseña.");
        }

        var cleanEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(cleanEmail);
        if (user == null)
        {
            return (false, "Token inválido o expirado.");
        }

        // 1. Validar hash SHA-256 en BD
        var tokenHash = ComputeSha256Hash(plainToken.Trim());
        var storedToken = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TokenHash == tokenHash, cancellationToken);

        if (storedToken == null)
        {
            logger.LogWarning("Token de recuperación no coincide para usuario {UserId}.", user.Id);
            return (false, "Token inválido o no reconocido.");
        }

        // 2. Validar expiración (15-30 minutos) y uso previo
        if (storedToken.IsUsed || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            db.PasswordResetTokens.Remove(storedToken);
            await db.SaveChangesAsync(cancellationToken);
            return (false, "El token de recuperación ha caducado. Solicita uno nuevo.");
        }

        // 3. Validar políticas de contraseña segura
        foreach (var validator in userManager.PasswordValidators)
        {
            var validationResult = await validator.ValidateAsync(userManager, user, newPassword);
            if (!validationResult.Succeeded)
            {
                var errors = string.Join(" ", validationResult.Errors.Select(e => e.Description));
                return (false, errors);
            }
        }

        // 4. Actualizar contraseña (hashing con PasswordHasher que usa PBKDF2/HMAC-SHA512 con alto factor de costo)
        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, newPassword);
        
        // 5. Invalidar tokens de sesión activos actualizando el SecurityStamp
        await userManager.UpdateSecurityStampAsync(user);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, "Error al actualizar la contraseña del usuario.");
        }

        // 6. Invalidar token consumido eliminándolo de la base de datos
        db.PasswordResetTokens.Remove(storedToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Contraseña restablecida exitosamente para {Email} y sesiones activas invalidadas.", cleanEmail);
        return (true, "Contraseña actualizada exitosamente.");
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
