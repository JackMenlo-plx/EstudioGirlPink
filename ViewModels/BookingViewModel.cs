using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using StudioGirlPink.Models;

namespace StudioGirlPink.ViewModels;

public class BookingViewModel
{
    // Datos de la Clienta
    [Required(ErrorMessage = "Tu nombre completo es obligatorio")]
    [StringLength(120, ErrorMessage = "El nombre no puede exceder 120 caracteres")]
    [Display(Name = "Nombre completo")]
    public string ClientFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tu número de teléfono es obligatorio")]
    [Phone(ErrorMessage = "Ingresa un número telefónico válido")]
    [StringLength(30)]
    [Display(Name = "Teléfono de contacto (WhatsApp)")]
    public string ClientPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido")]
    [StringLength(254)]
    [Display(Name = "Correo electrónico (opcional)")]
    public string? ClientEmail { get; set; }

    // Servicio y fecha solicitada
    [Required(ErrorMessage = "Selecciona un servicio")]
    [Display(Name = "Servicio")]
    public int ServiceId { get; set; }

    // Parámetros opcionales si viene referenciado desde Promociones o Cotizador
    public int? PromotionId { get; set; }
    public string? PromotionTitle { get; set; }
    public decimal? PresetPrice { get; set; }

    // Parámetros si viene referenciado desde la Galería de Trabajos ("Quiero un diseño así")
    public int? SelectedWorkId { get; set; }
    public string? SelectedWorkTitle { get; set; }
    public string? SelectedWorkCategory { get; set; }
    public string? SelectedWorkImagePath { get; set; }

    // Soporte para Slots Express (bloqueo dinámico con TTL de 15 min)
    public bool IsExpressSlot { get; set; } = false;

    [Required(ErrorMessage = "Selecciona la fecha para tu cita")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime SelectedDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Selecciona la hora para tu cita")]
    [Display(Name = "Hora")]
    public string SelectedTime { get; set; } = "11:00";

    // Variables de cotización dinámica
    [Range(1, 10, ErrorMessage = "El nivel de uñas debe estar entre 1 y 10")]
    [Display(Name = "Largo de Uñas Acrílicas (Nivel 1 al 10)")]
    public int? NailLengthLevel { get; set; } = 1;

    [Display(Name = "Largo de cabello")]
    public string? HairLength { get; set; } = "Hombro";

    [Display(Name = "¿Cabello con volumen abundante? (+$200 MXN)")]
    public bool HasExtraVolume { get; set; } = false;

    [StringLength(1000)]
    [Display(Name = "Notas o detalles de tu cita")]
    public string? ClientNotes { get; set; }

    // Archivos subidos: Comprobante de anticipo de $50 y foto de referencia
    [Required(ErrorMessage = "El comprobante de anticipo de $50 MXN es obligatorio para agendar.")]
    [Display(Name = "Comprobante de Anticipo ($50 MXN obligatorio)")]
    public IFormFile? DepositReceiptFile { get; set; }

    [Display(Name = "Fotografía de referencia o diseño deseado (opcional)")]
    public IFormFile? ReferencePhotoFile { get; set; }

    // Ficha opcional de diagnóstico capilar para alisado
    public bool FillHairDiagnosis { get; set; } = false;
    public bool HairHistoryNatural { get; set; }
    public bool HairHistoryDyed { get; set; }
    public bool HairHistoryBleached { get; set; }
    public bool HairHistoryKeratin { get; set; }
    public bool HairHistoryBotox { get; set; }
    public bool HasSplitEnds { get; set; }
    public bool IsDry { get; set; }
    public bool HasHairLoss { get; set; }
    public string HairThickness { get; set; } = "Medio";
    public string HairTexture { get; set; } = "Ondulado";
    public bool HasAllergies { get; set; }
    public bool HasSensitiveSkin { get; set; }
    public bool IsPregnantOrNursing { get; set; }
    public bool UsesDermatitisMedication { get; set; }
    public string HairDesiredGoal { get; set; } = "LisoTabla";

    // Lista de servicios para el dropdown
    public IReadOnlyList<Service>? AvailableServices { get; set; }
}
