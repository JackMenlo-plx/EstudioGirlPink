using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

/// <summary>
/// Modelo de reserva y cita en Studio Girl Pink.
/// Incluye lógica de anticipo obligatorio de $50 MXN, comprobante cargado y validación de horarios.
/// </summary>
public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    // Vinculación directa al usuario autenticado (RBAC: CLIENTE / ADMIN)
    public string? UserId { get; set; }

    // Soporte de Slots Express / Bloqueo Temporal con TTL
    public bool IsExpressSlot { get; set; } = false;
    public DateTime? SlotExpiresAt { get; set; }

    // Bloqueo manual por parte de Administrador (para mantenimiento, eventos o plantilla base)
    public bool IsAdminBlocked { get; set; } = false;

    [Required]
    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    // Fecha y hora de inicio
    [Required]
    public DateTime StartsAt { get; set; }

    // Duración estimada en minutos
    public int DurationMinutes { get; set; } = 90;

    // Hora estimada de finalización
    public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);

    // Variables de cotización dinámica
    [Range(1, 10)]
    public int? NailLengthLevel { get; set; } // Para Uñas Acrílicas (Nivel 1 al 10)

    [StringLength(40)]
    public string? HairLength { get; set; } // Hombro, Medio, Cintura, ExtraLargo

    public bool HasExtraVolume { get; set; } = false; // +$200 en Alisado

    // Cotización y anticipo
    [Range(0, 100000)]
    public decimal QuotedPrice { get; set; }

    [Range(0, 100000)]
    public decimal DepositAmount { get; set; } = 50.00m; // Fijo obligatorio de $50 MXN

    // Comprobante de anticipo cargado por la clienta
    [StringLength(300)]
    public string? DepositReceiptPath { get; set; }

    [Required, StringLength(30)]
    public string DepositStatus { get; set; } = "PendienteRevision"; // PendienteRevision, Aprobado, Rechazado

    // Estado de la cita según requerimiento
    [Required, StringLength(40)]
    public string Status { get; set; } = "PendienteConfirmacion"; // PendienteConfirmacion, Confirmada, RechazadaNoAsistio, Reprogramada, Completada

    // Foto de referencia para diseños personalizados
    [StringLength(300)]
    public string? ReferencePhotoPath { get; set; }

    // Descuento por Tarjeta de Lealtad (si aplica)
    public bool AppliedLoyaltyReward { get; set; } = false;

    [StringLength(100)]
    public string? LoyaltyRewardDescription { get; set; }

    [StringLength(1000)]
    public string? ClientNotes { get; set; }

    [StringLength(1000)]
    public string? AdminNotes { get; set; }

    // Motivo específico de rechazo / cancelación
    [StringLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
