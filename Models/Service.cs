using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

/// <summary>
/// Catálogo de servicios del estudio con reglas de cálculo y duración estimada.
/// </summary>
public class Service
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Category { get; set; } = "Uñas"; // Uñas, Pestañas y Cejas, Cabello, Pies, Especiales

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal BasePrice { get; set; }

    [Range(5, 600)]
    public int EstimatedMinutes { get; set; }

    [Required, StringLength(40)]
    public string PricingRuleType { get; set; } = "Fijo"; // Fijo, UñasNivel, AlisadoLargo, Personalizado

    public bool IsActive { get; set; } = true;
}
