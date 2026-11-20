using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

/// <summary>
/// Registro financiero de ingresos y gastos operativos del negocio.
/// </summary>
public class FinancialTransaction
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Type { get; set; } = "Ingreso"; // Ingreso, Gasto

    [Required, StringLength(50)]
    public string Category { get; set; } = "Servicio"; // Servicio, Materiales, Renta, Luz, Agua, Otros

    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required, StringLength(250)]
    public string Description { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
}
