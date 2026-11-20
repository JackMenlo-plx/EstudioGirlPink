using Microsoft.EntityFrameworkCore;
using StudioGirlPink.Data;

namespace StudioGirlPink.Services;

/// <summary>
/// Tarea en segundo plano (HostedService) que gestiona la expiración dinámica de Slots Express con TTL.
/// Libera automáticamente los horarios retenidos temporalmente si la clienta no completó la confirmación o pago.
/// </summary>
public class ExpressSlotCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpressSlotCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ExpressSlotCleanupService iniciado. Monitoreo de TTL cada {Minutes} min.", Interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await CleanupExpiredSlotsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error durante la limpieza de slots express expirados.");
            }
        }
    }

    private async Task CleanupExpiredSlotsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var expiredSlots = await db.Appointments
            .Where(a => a.IsExpressSlot && 
                        a.Status == "PendienteConfirmacion" && 
                        a.SlotExpiresAt != null && 
                        a.SlotExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredSlots.Count == 0) return;

        foreach (var slot in expiredSlots)
        {
            slot.Status = "Cancelada";
            slot.AdminNotes = $"Slot express liberado automáticamente por expiración de TTL ({slot.SlotExpiresAt:u}).";
            logger.LogInformation("Slot express #{Id} liberado por TTL expirado.", slot.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
