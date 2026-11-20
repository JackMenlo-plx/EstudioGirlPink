using System.ComponentModel.DataAnnotations;

namespace StudioGirlPink.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Tu nombre completo es obligatorio")]
    [StringLength(120)]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tu teléfono es obligatorio")]
    [Phone(ErrorMessage = "Ingresa un número telefónico válido")]
    [StringLength(30)]
    [Display(Name = "Teléfono de WhatsApp")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
