using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioGirlPink.Data;
using StudioGirlPink.Models;
using StudioGirlPink.Services;
using StudioGirlPink.ViewModels;

namespace StudioGirlPink.Controllers;

public class HomeController(
    AppDbContext db,
    PricingCalculatorService pricingCalculator,
    AppointmentScheduleService scheduleService,
    INotificationService notificationService,
    IRealtimeNotifier realtimeNotifier,
    IWebHostEnvironment environment) : Controller
{
    private readonly string[] _permittedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".pdf"];

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var services = await db.Services
            .Where(service => service.IsActive)
            .OrderBy(service => service.Id)
            .ToListAsync(cancellationToken);

        var works = await db.PortfolioWorks
            .OrderByDescending(w => w.CreatedAt)
            .Take(12)
            .ToListAsync(cancellationToken);

        var promotions = await db.Promotions
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        ViewBag.PortfolioWorks = works;
        ViewBag.Promotions = promotions;

        return View(services);
    }

    [HttpGet("cotizador")]
    public async Task<IActionResult> Cotizador(CancellationToken cancellationToken)
    {
        var services = await db.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        return View(services);
    }

    [HttpGet("api/cotizar")]
    public IActionResult CalcularCotizacion(
        string serviceName,
        int? nailLevel,
        string? hairLength,
        bool hasExtraVolume = false)
    {
        var result = pricingCalculator.Calculate(serviceName, nailLevel, hairLength, hasExtraVolume);
        return Json(new
        {
            success = true,
            basePrice = result.BasePrice,
            quotedPrice = result.QuotedPrice,
            depositAmount = result.DepositAmount,
            estimatedMinutes = result.EstimatedMinutes,
            breakdown = result.DetailsBreakdown
        });
    }

    [HttpGet("agendar")]
    public async Task<IActionResult> Agendar(
        int? serviceId, 
        int? nailLevel, 
        string? hairLength, 
        bool hasExtraVolume = false,
        int? promoId = null,
        int? workId = null,
        string? workTitle = null,
        string? workCategory = null,
        string? workImage = null,
        CancellationToken cancellationToken = default)
    {
        var services = await db.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        Promotion? promo = null;
        if (promoId.HasValue)
        {
            promo = await db.Promotions.FindAsync([promoId.Value], cancellationToken);
        }

        PortfolioWork? work = null;
        if (workId.HasValue)
        {
            work = await db.PortfolioWorks.FindAsync([workId.Value], cancellationToken);
        }

        var resolvedTitle = work?.Title ?? workTitle?.Trim();
        var resolvedCategory = work?.Category ?? workCategory?.Trim();
        var resolvedImage = work?.ImagePath ?? workImage;

        // Si no se pasó serviceId pero se seleccionó un trabajo de la galería, asociar inteligentemente
        var selectedServiceId = serviceId;
        if (!selectedServiceId.HasValue && !string.IsNullOrWhiteSpace(resolvedCategory))
        {
            var matched = services.FirstOrDefault(s => 
                s.Category.Equals(resolvedCategory, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(resolvedCategory, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                selectedServiceId = matched.Id;
            }
        }

        selectedServiceId ??= services.FirstOrDefault()?.Id ?? 1;

        var model = new BookingViewModel
        {
            ServiceId = selectedServiceId.Value,
            AvailableServices = services,
            SelectedDate = DateTime.Today.AddDays(1),
            NailLengthLevel = nailLevel ?? 1,
            HairLength = hairLength ?? "Hombro",
            HasExtraVolume = hasExtraVolume,
            PromotionId = promo?.Id,
            PromotionTitle = promo?.Title,
            PresetPrice = promo?.DiscountPrice,
            SelectedWorkId = work?.Id ?? workId,
            SelectedWorkTitle = resolvedTitle,
            SelectedWorkCategory = resolvedCategory,
            SelectedWorkImagePath = resolvedImage
        };

        if (promo != null)
        {
            model.ClientNotes = $"[PROMOCIÓN: {promo.Title} - Precio Promo: ${(promo.DiscountPrice.HasValue ? promo.DiscountPrice.Value.ToString("0.00") : "Especial")}]";
        }
        else if (!string.IsNullOrWhiteSpace(resolvedTitle))
        {
            model.ClientNotes = $"✨ Diseño seleccionado de la Galería de Trabajos: '{resolvedTitle}' (Categoría: {resolvedCategory ?? "Uñas"}). Deseo un diseño/acabado igual o similar a esta muestra de la galería del estudio.";
        }

        return View(model);
    }

    [HttpPost("agendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agendar(BookingViewModel model, CancellationToken cancellationToken)
    {
        var services = await db.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        model.AvailableServices = services;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var service = services.FirstOrDefault(s => s.Id == model.ServiceId);
        if (service == null)
        {
            ModelState.AddModelError(nameof(model.ServiceId), "El servicio seleccionado no es válido.");
            return View(model);
        }

        // Parsear fecha y hora seleccionada en zona horaria local de México
        if (!TimeSpan.TryParse(model.SelectedTime, out var timeOfDay))
        {
            ModelState.AddModelError(nameof(model.SelectedTime), "La hora seleccionada no es válida.");
            return View(model);
        }

        var localDateTime = model.SelectedDate.Date.Add(timeOfDay);
        var proposedStartUtc = TimeZoneHelper.MexicoToUtc(localDateTime);

        // Calcular precio y duración
        var quote = pricingCalculator.Calculate(
            service.Name,
            model.NailLengthLevel,
            model.HairLength,
            model.HasExtraVolume);

        // Obtener citas activas en esa fecha para validar traslapes y margen de 90 min
        var dayStartUtc = proposedStartUtc.Date;
        var dayEndUtc = dayStartUtc.AddDays(1);
        var existingAppointments = await db.Appointments
            .Where(a => a.StartsAt >= dayStartUtc && a.StartsAt < dayEndUtc)
            .ToListAsync(cancellationToken);

        // Validar horarios comerciales y separación de 1.5 hrs entre clientes
        var scheduleCheck = scheduleService.ValidateSlot(
            model.SelectedDate.Date,
            timeOfDay,
            quote.EstimatedMinutes,
            proposedStartUtc,
            existingAppointments,
            isAdminOverride: false);

        if (!scheduleCheck.IsValid)
        {
            ModelState.AddModelError(string.Empty, scheduleCheck.ErrorMessage ?? "Conflicto en la agenda.");
            return View(model);
        }

        // Subida obligatoria de Comprobante de anticipo
        if (model.DepositReceiptFile == null || model.DepositReceiptFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.DepositReceiptFile), "El comprobante de anticipo de $50 MXN es obligatorio.");
            return View(model);
        }

        var receiptPath = await SaveUploadedFileAsync(model.DepositReceiptFile, "receipts");
        if (receiptPath == null)
        {
            ModelState.AddModelError(nameof(model.DepositReceiptFile), "Formato de comprobante inválido. Usa JPG, PNG, WEBP o PDF.");
            return View(model);
        }

        string? referencePhotoPath = null;
        if (model.ReferencePhotoFile != null && model.ReferencePhotoFile.Length > 0)
        {
            referencePhotoPath = await SaveUploadedFileAsync(model.ReferencePhotoFile, "references");
        }
        else if (!string.IsNullOrWhiteSpace(model.SelectedWorkImagePath))
        {
            referencePhotoPath = model.SelectedWorkImagePath;
        }

        // Localizar o crear Clienta
        var clientPhone = model.ClientPhone.Trim();
        var client = await db.Clients
            .Include(c => c.HairDiagnosis)
            .FirstOrDefaultAsync(c => c.Phone == clientPhone, cancellationToken);

        if (client == null)
        {
            client = new Client
            {
                FullName = model.ClientFullName.Trim(),
                Phone = clientPhone,
                Email = string.IsNullOrWhiteSpace(model.ClientEmail) ? null : model.ClientEmail.Trim(),
                LoyaltyCount = 0,
                TotalSpent = 0m,
                CreatedAt = DateTime.UtcNow
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync(cancellationToken);
        }

        // Guardar o actualizar Ficha de Diagnóstico Capilar si se solicitó
        if (model.FillHairDiagnosis)
        {
            if (client.HairDiagnosis == null)
            {
                client.HairDiagnosis = new HairDiagnosis
                {
                    ClientId = client.Id,
                    HistoryNatural = model.HairHistoryNatural,
                    HistoryDyed = model.HairHistoryDyed,
                    HistoryBleached = model.HairHistoryBleached,
                    HistoryKeratin = model.HairHistoryKeratin,
                    HistoryBotox = model.HairHistoryBotox,
                    HasSplitEnds = model.HasSplitEnds,
                    IsDry = model.IsDry,
                    HasHairLoss = model.HasHairLoss,
                    Thickness = model.HairThickness,
                    Texture = model.HairTexture,
                    HasAllergies = model.HasAllergies,
                    HasSensitiveSkin = model.HasSensitiveSkin,
                    IsPregnantOrNursing = model.IsPregnantOrNursing,
                    UsesDermatitisMedication = model.UsesDermatitisMedication,
                    DesiredGoal = model.HairDesiredGoal,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                client.HairDiagnosis.HistoryNatural = model.HairHistoryNatural;
                client.HairDiagnosis.HistoryDyed = model.HairHistoryDyed;
                client.HairDiagnosis.HistoryBleached = model.HairHistoryBleached;
                client.HairDiagnosis.HistoryKeratin = model.HairHistoryKeratin;
                client.HairDiagnosis.HistoryBotox = model.HairHistoryBotox;
                client.HairDiagnosis.HasSplitEnds = model.HasSplitEnds;
                client.HairDiagnosis.IsDry = model.IsDry;
                client.HairDiagnosis.HasHairLoss = model.HasHairLoss;
                client.HairDiagnosis.Thickness = model.HairThickness;
                client.HairDiagnosis.Texture = model.HairTexture;
                client.HairDiagnosis.HasAllergies = model.HasAllergies;
                client.HairDiagnosis.HasSensitiveSkin = model.HasSensitiveSkin;
                client.HairDiagnosis.IsPregnantOrNursing = model.IsPregnantOrNursing;
                client.HairDiagnosis.UsesDermatitisMedication = model.UsesDermatitisMedication;
                client.HairDiagnosis.DesiredGoal = model.HairDesiredGoal;
                client.HairDiagnosis.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Si la cita viene con una promoción activa con precio de descuento especial, respetarlo
        decimal finalQuotedPrice = quote.QuotedPrice;
        if (model.PresetPrice.HasValue && model.PresetPrice.Value > 0)
        {
            finalQuotedPrice = model.PresetPrice.Value;
        }

        // Extraer UserId de la sesión actual (vía Cookie de Identity o JWT vía ProtectMiddleware / req.user.id)
        var authenticatedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? HttpContext.Items["userId"]?.ToString();

        // Crear la cita en estado "Pendiente de Confirmación" (requiere aprobación del Admin)
        var appointment = new Appointment
        {
            ClientId = client.Id,
            UserId = authenticatedUserId,
            IsExpressSlot = model.IsExpressSlot,
            SlotExpiresAt = model.IsExpressSlot ? DateTime.UtcNow.AddMinutes(15) : null,
            ServiceId = service.Id,
            StartsAt = proposedStartUtc,
            DurationMinutes = quote.EstimatedMinutes,
            NailLengthLevel = model.NailLengthLevel,
            HairLength = model.HairLength,
            HasExtraVolume = model.HasExtraVolume,
            QuotedPrice = finalQuotedPrice,
            DepositAmount = quote.DepositAmount,
            DepositReceiptPath = receiptPath,
            DepositStatus = receiptPath != null ? "PendienteRevision" : "SinComprobante",
            Status = "PendienteConfirmacion",
            ReferencePhotoPath = referencePhotoPath,
            ClientNotes = model.ClientNotes,
            CreatedAt = DateTime.UtcNow
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        // Notificación en Tiempo Real vía SignalR a la administración
        var localStart = appointment.StartsAt.ToMexicoTime();
        await realtimeNotifier.NotifyNewAppointmentAsync(new RealtimeAppointmentDto(
            Id: appointment.Id,
            ClientName: client.FullName,
            ClientPhone: client.Phone,
            ServiceName: service.Name,
            StartsAtFormatted: localStart.ToString("dd/MM/yyyy hh:mm tt"),
            Duration: $"{appointment.DurationMinutes} min",
            QuotedPrice: appointment.QuotedPrice,
            DepositAmount: appointment.DepositAmount,
            DepositStatus: appointment.DepositStatus,
            Status: appointment.Status,
            Notes: appointment.ClientNotes,
            ReceiptPath: appointment.DepositReceiptPath,
            ReferencePhotoPath: appointment.ReferencePhotoPath,
            IsExpressSlot: appointment.IsExpressSlot,
            TimeAgo: "Hace unos segundos"
        ));

        // Notificar en consola/sistema la recepción de cita y comprobante
        await notificationService.SendStatusNotificationAsync(
            client.Phone,
            client.FullName,
            appointment.Id,
            "NuevaSolicitud",
            null,
            appointment.StartsAt,
            service.Name);

        return RedirectToAction(nameof(ConfirmacionCita), new { id = appointment.Id });
    }

    [HttpGet("confirmacion-cita/{id:int}")]
    public async Task<IActionResult> ConfirmacionCita(int id, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (appointment == null)
        {
            return NotFound();
        }

        var quote = pricingCalculator.Calculate(
            appointment.Service?.Name ?? string.Empty,
            appointment.NailLengthLevel,
            appointment.HairLength,
            appointment.HasExtraVolume);

        var viewModel = new AppointmentConfirmationViewModel
        {
            AppointmentId = appointment.Id,
            ClientName = appointment.Client?.FullName ?? "Clienta",
            ClientPhone = appointment.Client?.Phone ?? "",
            ServiceName = appointment.Service?.Name ?? "Servicio",
            StartsAt = appointment.StartsAt,
            DurationMinutes = appointment.DurationMinutes,
            QuotedPrice = appointment.QuotedPrice,
            DepositAmount = appointment.DepositAmount,
            DepositStatus = appointment.DepositStatus,
            Status = appointment.Status,
            HasReceipt = !string.IsNullOrEmpty(appointment.DepositReceiptPath),
            PriceBreakdown = quote.DetailsBreakdown
        };

        return View(viewModel);
    }

    [HttpGet("consultar-cita")]
    public async Task<IActionResult> ConsultarCita(string? telefono, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telefono))
        {
            return View();
        }

        var cleanPhone = telefono.Trim();
        var appointments = await db.Appointments
            .Include(a => a.Service)
            .Include(a => a.Client)
            .Where(a => a.Client != null && a.Client.Phone == cleanPhone)
            .OrderByDescending(a => a.StartsAt)
            .ToListAsync(cancellationToken);

        ViewBag.TelefonoBuscado = cleanPhone;
        return View(appointments);
    }

    [Route("Home/Error")]
    public IActionResult Error()
    {
        return View("Error");
    }

    private async Task<string?> SaveUploadedFileAsync(IFormFile file, string subFolder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_permittedExtensions.Contains(ext))
        {
            return null;
        }

        var uploadsFolder = Path.Combine(environment.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/{uniqueFileName}";
    }
}
