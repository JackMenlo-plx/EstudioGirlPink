using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

/// <summary>
/// Ficha de Diagnóstico Capilar para clientas que solicitan Alisado Progresivo u otros tratamientos.
/// </summary>
public class HairDiagnosis
{
    public int Id { get; set; }

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    // Historial capilar previo
    public bool HistoryNatural { get; set; } = false;
    public bool HistoryDyed { get; set; } = false;
    public bool HistoryBleached { get; set; } = false;
    public bool HistoryKeratin { get; set; } = false;
    public bool HistoryBotox { get; set; } = false;

    [StringLength(500)]
    public string? HistoryNotes { get; set; }

    // Estado actual del cabello
    public bool HasSplitEnds { get; set; } = false;
    public bool IsDry { get; set; } = false;
    public bool HasHairLoss { get; set; } = false;

    [Required, StringLength(30)]
    public string Thickness { get; set; } = "Medio"; // Delgado, Medio, Grueso

    [Required, StringLength(30)]
    public string Texture { get; set; } = "Ondulado"; // Lacio, Ondulado, Chino, Rizado

    // Condiciones especiales y médicas
    public bool HasAllergies { get; set; } = false;
    public bool HasSensitiveSkin { get; set; } = false;
    public bool IsPregnantOrNursing { get; set; } = false;
    public bool UsesDermatitisMedication { get; set; } = false;

    [StringLength(500)]
    public string? SpecialConditionsNotes { get; set; }

    // Objetivo de la clienta
    [Required, StringLength(50)]
    public string DesiredGoal { get; set; } = "LisoTabla"; // LisoTabla, ReduccionFrizz, BrilloYFuerza

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
