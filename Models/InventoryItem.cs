using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

/// <summary>
/// Control de insumos y materiales del estudio con alertas de reposición y control de merma.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Category { get; set; } = "Insumos"; // Acrílicos, Geles, Monómeros, Herramientas, Químicos, Desechables

    [Range(0, 50000)]
    public decimal CurrentStock { get; set; }

    [Range(0, 50000)]
    public decimal MinimumStock { get; set; } // Umbral para alerta automática

    [Required, StringLength(30)]
    public string Unit { get; set; } = "Piezas"; // Piezas, ml, gr, paquetes

    [Range(0, 100000)]
    public decimal UnitCost { get; set; }

    [Range(0, 50000)]
    public decimal DamagedOrDiscardedStock { get; set; } = 0; // Registro de mermas/deterioro

    public bool IsLowStock => CurrentStock <= MinimumStock;

    public DateTime LastRestockedAt { get; set; } = DateTime.UtcNow;
}
