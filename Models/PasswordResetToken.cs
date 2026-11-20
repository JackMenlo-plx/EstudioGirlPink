using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace StudioGirlPink.Models;

/// <summary>
/// Almacena tokens criptográficos de recuperación de contraseña con hash SHA-256.
/// Implementa tiempo de vida estricto (15-30 minutos) e invalidación al consumirse.
/// </summary>
public class PasswordResetToken
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    [Required, StringLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; } = false;
}
