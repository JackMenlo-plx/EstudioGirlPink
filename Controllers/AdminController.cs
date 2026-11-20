using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioGirlPink.Data;
using StudioGirlPink.Models;
using StudioGirlPink.Services;

namespace StudioGirlPink.Controllers;

[Authorize(Roles = "Administrator")]
[Route("admin")]
public class AdminController(
    AppDbContext db,
    LoyaltyService loyaltyService,
    INotificationService notificationService,
    IRealtimeNotifier realtimeNotifier,
    IWebHostEnvironment environment) : Controller
{
    private readonly string[] _permittedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    // ==========================================
    // 1. DASHBOARD PRINCIPAL DE ADMINISTRACIÓN
    // ==========================================
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var pendingAppointments = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Service)
            .Where(a => a.Status == "PendienteConfirmacion")
            .OrderBy(a => a.StartsAt)
            .ToListAsync(cancellationToken);

        var todayStartMexico = TimeZoneHelper.ToMexicoTime(DateTime.UtcNow).Date;
        var todayStartUtc = TimeZoneHelper.MexicoToUtc(todayStartMexico);
        var todayEndUtc = todayStartUtc.AddDays(1);

        var todayAppointments = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Service)
            .Where(a => a.StartsAt >= todayStartUtc && a.StartsAt < todayEndUtc && a.Status == "Confirmada")
            .OrderBy(a => a.StartsAt)
            .ToListAsync(cancellationToken);

        var lowStockItems = await db.InventoryItems
            .Where(i => i.CurrentStock <= i.MinimumStock)
            .ToListAsync(cancellationToken);

        var totalRevenue = await db.FinancialTransactions
            .Where(t => t.Type == "Ingreso")
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var totalExpenses = await db.FinancialTransactions
            .Where(t => t.Type == "Gasto")
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        ViewBag.PendingCount = pendingAppointments.Count;
        ViewBag.PendingAppointments = pendingAppointments;
        ViewBag.TodayAppointments = todayAppointments;
        ViewBag.LowStockItems = lowStockItems;
        ViewBag.TotalClients = await db.Clients.CountAsync(cancellationToken);
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.NetProfit = totalRevenue - totalExpenses;

        return View();
    }

    // ==========================================
    // 2. GESTIÓN INTEGRAL DE CITAS Y VALIDACIÓN
    // ==========================================
    [HttpGet("citas")]
    public async Task<IActionResult> Citas(string? statusFilter, CancellationToken cancellationToken)
    {
        var query = db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Service)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(a => a.Status == statusFilter);
        }

        var appointments = await query
            .OrderByDescending(a => a.StartsAt)
            .ToListAsync(cancellationToken);

        ViewBag.CurrentFilter = statusFilter;
        return View(appointments);
    }

    [HttpPost("citas/confirmar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarCita(int id, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (appointment == null) return NotFound();

        appointment.Status = "Confirmada";
        appointment.DepositStatus = "Aprobado";
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyAppointmentStatusChangedAsync(id, "Confirmada");

        if (appointment.Client != null)
        {
            await notificationService.SendStatusNotificationAsync(
                appointment.Client.Phone,
                appointment.Client.FullName,
                appointment.Id,
                "Confirmada",
                null,
                appointment.StartsAt,
                appointment.Service?.Name);

            var waLink = notificationService.GenerateWhatsAppDirectLink(
                appointment.Client.Phone,
                appointment.Client.FullName,
                appointment.Id,
                "Confirmada",
                null,
                appointment.StartsAt,
                appointment.Service?.Name);

            TempData["SuccessMessage"] = $"Cita #{id} aprobada y confirmada exitosamente. Se preparó la notificación para el número {appointment.Client.Phone}.";
            TempData["WhatsAppLink"] = waLink;
        }
        else
        {
            TempData["SuccessMessage"] = $"Cita #{id} aprobada y confirmada.";
        }

        return RedirectToAction(nameof(Citas));
    }

    [HttpPost("citas/rechazar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RechazarCita(int id, string? reason, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (appointment == null) return NotFound();

        var motivo = string.IsNullOrWhiteSpace(reason)
            ? "Comprobante no válido o fecha/hora no disponible."
            : reason.Trim();

        appointment.Status = "Rechazada";
        appointment.DepositStatus = "Rechazado";
        appointment.RejectionReason = motivo;
        appointment.AdminNotes = motivo;
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyAppointmentStatusChangedAsync(id, "Rechazada", motivo);

        if (appointment.Client != null)
        {
            await notificationService.SendStatusNotificationAsync(
                appointment.Client.Phone,
                appointment.Client.FullName,
                appointment.Id,
                "Rechazada",
                motivo,
                appointment.StartsAt,
                appointment.Service?.Name);

            var waLink = notificationService.GenerateWhatsAppDirectLink(
                appointment.Client.Phone,
                appointment.Client.FullName,
                appointment.Id,
                "Rechazada",
                motivo,
                appointment.StartsAt,
                appointment.Service?.Name);

            TempData["SuccessMessage"] = $"Cita #{id} rechazada con el motivo: '{motivo}'. Notificación lista para el teléfono {appointment.Client.Phone}.";
            TempData["WhatsAppLink"] = waLink;
        }
        else
        {
            TempData["SuccessMessage"] = $"Cita #{id} rechazada con motivo registrado.";
        }

        return RedirectToAction(nameof(Citas));
    }

    [HttpPost("citas/eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCita(int id, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.FindAsync([id], cancellationToken);
        if (appointment == null) return NotFound();

        // Si tenía transacciones financieras asociadas, eliminarlas o desvincularlas
        var relatedTxs = await db.FinancialTransactions
            .Where(t => t.AppointmentId == id)
            .ToListAsync(cancellationToken);

        if (relatedTxs.Count > 0)
        {
            db.FinancialTransactions.RemoveRange(relatedTxs);
        }

        db.Appointments.Remove(appointment);
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyAppointmentStatusChangedAsync(id, "Eliminada");

        TempData["SuccessMessage"] = $"La cita #{id} ha sido eliminada permanentemente del sistema.";
        return RedirectToAction(nameof(Citas));
    }

    // ==========================================
    // CONTROL ADMINISTRATIVO DE HORARIOS (SLOTS)
    // ==========================================
    [HttpPost("citas/bloquear-horario")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BloquearHorario(
        DateTime selectedDate,
        string selectedTime,
        int durationMinutes,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (!TimeSpan.TryParse(selectedTime, out var timeOfDay))
        {
            TempData["ErrorMessage"] = "Hora seleccionada no válida.";
            return RedirectToAction(nameof(Citas));
        }

        var localDateTime = selectedDate.Date.Add(timeOfDay);
        var startUtc = TimeZoneHelper.MexicoToUtc(localDateTime);

        var defaultService = await db.Services.FirstOrDefaultAsync(cancellationToken);
        if (defaultService == null)
        {
            TempData["ErrorMessage"] = "No existen servicios dados de alta para asociar el bloqueo.";
            return RedirectToAction(nameof(Citas));
        }

        var systemClient = await db.Clients.FirstOrDefaultAsync(c => c.Phone == "0000000000", cancellationToken);
        if (systemClient == null)
        {
            systemClient = new Client
            {
                FullName = "Estudio Girl Pink (Bloqueo Admin)",
                Phone = "0000000000",
                Email = "admin@studiogirlpink.com",
                CreatedAt = DateTime.UtcNow
            };
            db.Clients.Add(systemClient);
            await db.SaveChangesAsync(cancellationToken);
        }

        var blockedSlot = new Appointment
        {
            ClientId = systemClient.Id,
            ServiceId = defaultService.Id,
            StartsAt = startUtc,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : 90,
            IsAdminBlocked = true,
            Status = "BloqueadoAdmin",
            DepositStatus = "BloqueadoAdmin",
            ClientNotes = string.IsNullOrWhiteSpace(reason) ? "Horario reservado/bloqueado por administración." : reason,
            CreatedAt = DateTime.UtcNow
        };

        db.Appointments.Add(blockedSlot);
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Horario {selectedDate:dd/MM/yyyy} a las {selectedTime} bloqueado con éxito.";
        return RedirectToAction(nameof(Citas));
    }

    [HttpPost("citas/desbloquear-horario/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesbloquearHorario(int id, CancellationToken cancellationToken)
    {
        var slot = await db.Appointments.FindAsync([id], cancellationToken);
        if (slot == null) return NotFound();

        if (slot.IsAdminBlocked || slot.Status == "BloqueadoAdmin")
        {
            db.Appointments.Remove(slot);
            await db.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "El horario ha sido desbloqueado y queda disponible para reservas.";
        }
        else
        {
            TempData["ErrorMessage"] = "El registro seleccionado no corresponde a un bloqueo administrativo.";
        }

        return RedirectToAction(nameof(Citas));
    }

    [HttpPost("citas/liberar-express/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LiberarSlotExpress(int id, CancellationToken cancellationToken)
    {
        var slot = await db.Appointments.FindAsync([id], cancellationToken);
        if (slot == null) return NotFound();

        if (slot.IsExpressSlot && slot.Status == "PendienteConfirmacion")
        {
            db.Appointments.Remove(slot);
            await db.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = $"Slot express #{id} liberado manualmente. El horario está ahora abierto a otras clientas.";
        }
        else
        {
            TempData["ErrorMessage"] = "El registro seleccionado no es un slot express pendiente.";
        }

        return RedirectToAction(nameof(Citas));
    }

    [HttpPost("citas/completar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletarCita(int id, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (appointment == null) return NotFound();

        appointment.Status = "Completada";

        // Actualizar fidelización y gastos acumulados de la clienta
        if (appointment.Client != null)
        {
            appointment.Client.LoyaltyCount += 1;
            appointment.Client.TotalSpent += appointment.QuotedPrice;
        }

        // Registrar ingreso financiero en caja
        var income = new FinancialTransaction
        {
            Type = "Ingreso",
            Category = "Servicio",
            Amount = appointment.QuotedPrice,
            Description = $"Servicio {appointment.Service?.Name ?? "General"} completado para {appointment.Client?.FullName ?? "Cliente"} (Cita #{appointment.Id})",
            TransactionDate = DateTime.UtcNow,
            AppointmentId = appointment.Id
        };
        db.FinancialTransactions.Add(income);

        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Cita #{id} finalizada. Se sumó la visita a la tarjeta de lealtad y se registró el ingreso de ${appointment.QuotedPrice:0.00} MXN.";
        return RedirectToAction(nameof(Citas));
    }

    // ==========================================
    // 3. MÓDULO DE CLIENTAS Y DIAGNÓSTICO CAPILAR
    // ==========================================
    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes(CancellationToken cancellationToken)
    {
        var clients = await db.Clients
            .Include(c => c.Appointments)
            .Include(c => c.HairDiagnosis)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(clients);
    }

    [HttpGet("clientes/{id:int}")]
    public async Task<IActionResult> DetalleCliente(int id, CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .Include(c => c.Appointments)
                .ThenInclude(a => a.Service)
            .Include(c => c.HairDiagnosis)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (client == null) return NotFound();

        var loyalty = loyaltyService.CheckLoyaltyStatus(client.LoyaltyCount);
        ViewBag.LoyaltyBenefit = loyalty;

        return View(client);
    }

    // ==========================================
    // 4. MÓDULO DE INVENTARIO (LIMPIAR, AGREGAR, ELIMINAR)
    // ==========================================
    [HttpGet("inventario")]
    public async Task<IActionResult> Inventario(CancellationToken cancellationToken)
    {
        var items = await db.InventoryItems
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);

        return View(items);
    }

    [HttpPost("inventario/agregar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarInsumo(
        string name,
        string category,
        decimal currentStock,
        decimal minimumStock,
        string unit,
        decimal unitCost,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "El nombre del insumo es obligatorio.";
            return RedirectToAction(nameof(Inventario));
        }

        var item = new InventoryItem
        {
            Name = name.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
            CurrentStock = Math.Max(0, currentStock),
            MinimumStock = Math.Max(0, minimumStock),
            Unit = string.IsNullOrWhiteSpace(unit) ? "Piezas" : unit.Trim(),
            UnitCost = Math.Max(0, unitCost),
            DamagedOrDiscardedStock = 0,
            LastRestockedAt = DateTime.UtcNow
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Insumo '{item.Name}' agregado correctamente al inventario.";
        return RedirectToAction(nameof(Inventario));
    }

    [HttpPost("inventario/eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarInsumo(int id, CancellationToken cancellationToken)
    {
        var item = await db.InventoryItems.FindAsync([id], cancellationToken);
        if (item == null) return NotFound();

        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Insumo '{item.Name}' eliminado del inventario.";
        return RedirectToAction(nameof(Inventario));
    }

    [HttpPost("inventario/borrar-todo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BorrarTodoInventario(CancellationToken cancellationToken)
    {
        var allItems = await db.InventoryItems.ToListAsync(cancellationToken);
        db.InventoryItems.RemoveRange(allItems);
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Se vació todo el inventario de prueba exitosamente. Ahora puedes agregar tus propios insumos.";
        return RedirectToAction(nameof(Inventario));
    }

    [HttpPost("inventario/ajustar-stock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjustarStock(int id, decimal delta, CancellationToken cancellationToken)
    {
        var item = await db.InventoryItems.FindAsync([id], cancellationToken);
        if (item == null) return NotFound();

        item.CurrentStock = Math.Max(0, item.CurrentStock + delta);
        item.LastRestockedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Stock de {item.Name} actualizado a {item.CurrentStock} {item.Unit}.";
        return RedirectToAction(nameof(Inventario));
    }

    [HttpPost("inventario/registrar-merma")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarMerma(int id, decimal quantity, CancellationToken cancellationToken)
    {
        var item = await db.InventoryItems.FindAsync([id], cancellationToken);
        if (item == null) return NotFound();

        item.CurrentStock = Math.Max(0, item.CurrentStock - quantity);
        item.DamagedOrDiscardedStock += quantity;
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Se registraron {quantity} {item.Unit} de merma/deterioro para {item.Name}.";
        return RedirectToAction(nameof(Inventario));
    }

    // ==========================================
    // 5. MÓDULO DE FINANZAS Y REINICIO DE VENTAS
    // ==========================================
    [HttpGet("finanzas")]
    public async Task<IActionResult> Finanzas(CancellationToken cancellationToken)
    {
        var transactions = await db.FinancialTransactions
            .Include(t => t.Appointment)
            .OrderByDescending(t => t.TransactionDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        var totalIngresos = await db.FinancialTransactions
            .Where(t => t.Type == "Ingreso")
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var totalGastos = await db.FinancialTransactions
            .Where(t => t.Type == "Gasto")
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        ViewBag.TotalIngresos = totalIngresos;
        ViewBag.TotalGastos = totalGastos;
        ViewBag.BalanceNeto = totalIngresos - totalGastos;

        return View(transactions);
    }

    [HttpPost("finanzas/registrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarTransaccion(
        string type,
        string category,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(description))
        {
            TempData["ErrorMessage"] = "Monto y descripción son obligatorios.";
            return RedirectToAction(nameof(Finanzas));
        }

        var transaction = new FinancialTransaction
        {
            Type = type == "Gasto" ? "Gasto" : "Ingreso",
            Category = string.IsNullOrWhiteSpace(category) ? "Operación" : category,
            Amount = amount,
            Description = description.Trim(),
            TransactionDate = DateTime.UtcNow
        };

        db.FinancialTransactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Transacción de {transaction.Type} por ${transaction.Amount:0.00} MXN registrada.";
        return RedirectToAction(nameof(Finanzas));
    }

    [HttpPost("finanzas/reiniciar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReiniciarFinanzas(CancellationToken cancellationToken)
    {
        var transactions = await db.FinancialTransactions.ToListAsync(cancellationToken);
        db.FinancialTransactions.RemoveRange(transactions);

        // También reiniciar el TotalSpent de clientes de prueba si se desea empezar limpio
        var clients = await db.Clients.ToListAsync(cancellationToken);
        foreach (var c in clients)
        {
            c.TotalSpent = 0m;
        }

        await db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Ventas y transacciones financieras reiniciadas a $0.00 MXN con éxito.";
        return RedirectToAction(nameof(Finanzas));
    }

    // ==========================================
    // 6. MÓDULO DE TRABAJOS (PORTAFOLIO)
    // ==========================================
    [HttpGet("trabajos")]
    public async Task<IActionResult> Trabajos(CancellationToken cancellationToken)
    {
        var works = await db.PortfolioWorks
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(works);
    }

    [HttpPost("trabajos/subir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirTrabajo(
        string title,
        string category,
        string? description,
        IFormFile? photo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || photo == null || photo.Length == 0)
        {
            TempData["ErrorMessage"] = "Título y fotografía del trabajo son obligatorios.";
            return RedirectToAction(nameof(Trabajos));
        }

        var photoPath = await SaveImageFileAsync(photo, "works");
        if (photoPath == null)
        {
            TempData["ErrorMessage"] = "Formato de imagen inválido. Usa JPG, PNG o WEBP.";
            return RedirectToAction(nameof(Trabajos));
        }

        var work = new PortfolioWork
        {
            Title = title.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "Uñas" : category.Trim(),
            Description = description?.Trim(),
            ImagePath = photoPath,
            CreatedAt = DateTime.UtcNow
        };

        db.PortfolioWorks.Add(work);
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyPortfolioChangedAsync(new RealtimeWorkDto(
            work.Id,
            work.Title,
            work.Category,
            work.Description,
            work.ImagePath,
            "created"));

        TempData["SuccessMessage"] = $"Trabajo '{work.Title}' agregado a la galería pública.";
        return RedirectToAction(nameof(Trabajos));
    }

    [HttpPost("trabajos/eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTrabajo(int id, CancellationToken cancellationToken)
    {
        var work = await db.PortfolioWorks.FindAsync([id], cancellationToken);
        if (work == null) return NotFound();

        db.PortfolioWorks.Remove(work);
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyPortfolioChangedAsync(new RealtimeWorkDto(
            work.Id,
            work.Title,
            work.Category,
            work.Description,
            work.ImagePath,
            "deleted"));

        TempData["SuccessMessage"] = $"Trabajo '{work.Title}' eliminado.";
        return RedirectToAction(nameof(Trabajos));
    }

    // ==========================================
    // 7. MÓDULO DE PROMOCIONES
    // ==========================================
    [HttpGet("promociones")]
    public async Task<IActionResult> Promociones(CancellationToken cancellationToken)
    {
        var promos = await db.Promotions
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(promos);
    }

    [HttpPost("promociones/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPromocion(
        string title,
        string description,
        string? badgeText,
        decimal? discountPrice,
        DateTime? validUntil,
        IFormFile? bannerPhoto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
        {
            TempData["ErrorMessage"] = "Título y descripción de la promoción son obligatorios.";
            return RedirectToAction(nameof(Promociones));
        }

        string? imagePath = null;
        if (bannerPhoto != null && bannerPhoto.Length > 0)
        {
            imagePath = await SaveImageFileAsync(bannerPhoto, "promotions");
        }

        var promo = new Promotion
        {
            Title = title.Trim(),
            Description = description.Trim(),
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? "PROMO" : badgeText.Trim(),
            DiscountPrice = discountPrice,
            ValidUntil = validUntil.HasValue ? DateTime.SpecifyKind(validUntil.Value, DateTimeKind.Utc) : null,
            ImagePath = imagePath,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Promotions.Add(promo);
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyPromotionChangedAsync(new RealtimePromotionDto(
            promo.Id,
            promo.Title,
            promo.Description,
            promo.BadgeText,
            promo.DiscountPrice,
            promo.IsActive,
            promo.ImagePath,
            "created"));

        TempData["SuccessMessage"] = $"Promoción '{promo.Title}' publicada con éxito.";
        return RedirectToAction(nameof(Promociones));
    }

    [HttpPost("promociones/cambiar-estado/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoPromocion(int id, CancellationToken cancellationToken)
    {
        var promo = await db.Promotions.FindAsync([id], cancellationToken);
        if (promo == null) return NotFound();

        promo.IsActive = !promo.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyPromotionChangedAsync(new RealtimePromotionDto(
            promo.Id,
            promo.Title,
            promo.Description,
            promo.BadgeText,
            promo.DiscountPrice,
            promo.IsActive,
            promo.ImagePath,
            promo.IsActive ? "activated" : "deactivated"));

        TempData["SuccessMessage"] = $"Promoción '{promo.Title}' ahora está {(promo.IsActive ? "Activa" : "Pausada")}.";
        return RedirectToAction(nameof(Promociones));
    }

    [HttpPost("promociones/eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarPromocion(int id, CancellationToken cancellationToken)
    {
        var promo = await db.Promotions.FindAsync([id], cancellationToken);
        if (promo == null) return NotFound();

        db.Promotions.Remove(promo);
        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyPromotionChangedAsync(new RealtimePromotionDto(
            promo.Id,
            promo.Title,
            promo.Description,
            promo.BadgeText,
            promo.DiscountPrice,
            false,
            promo.ImagePath,
            "deleted"));

        TempData["SuccessMessage"] = $"Promoción '{promo.Title}' eliminada.";
        return RedirectToAction(nameof(Promociones));
    }

    private async Task<string?> SaveImageFileAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_permittedImageExtensions.Contains(ext))
        {
            return null;
        }

        var targetFolder = Path.Combine(environment.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(targetFolder);

        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(targetFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{folder}/{uniqueName}";
    }
}
