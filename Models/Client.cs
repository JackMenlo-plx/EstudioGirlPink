using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

/// <summary>
/// Representa a una clienta de Studio Girl Pink, incluyendo su historial y tarjeta de lealtad.
/// </summary>
public class Client
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    // Contador de visitas para la Tarjeta de Lealtad (5 servicios = 6to con Gel Semipermanente gratis o 50% desc.)
    public int LoyaltyCount { get; set; } = 0;

    // Total acumulado gastado en el estudio
    public decimal TotalSpent { get; set; } = 0;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public HairDiagnosis? HairDiagnosis { get; set; }
}
