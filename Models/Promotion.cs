using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.Models;

public class Promotion
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(80)]
    public string? BadgeText { get; set; } = "PROMO"; // ej: 2x1, 20% OFF, Especial

    [Range(0, 10000)]
    public decimal? DiscountPrice { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(300)]
    public string? ImagePath { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
