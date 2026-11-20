using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

public class PortfolioWork
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Category { get; set; } = "Uñas"; // Uñas, Cabello, Pestañas, Cejas, Pies

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(300)]
    public string ImagePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
