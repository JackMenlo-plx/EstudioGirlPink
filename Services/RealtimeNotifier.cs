using Microsoft.AspNetCore.SignalR;
using StudioGirlPink.Hubs;

namespace StudioGirlPink.Services;

public record RealtimeAppointmentDto(
    int Id,
    string ClientName,
    string ClientPhone,
    string ServiceName,
    string StartsAtFormatted,
    string Duration,
    decimal QuotedPrice,
    decimal DepositAmount,
    string DepositStatus,
    string Status,
    string? Notes,
    string? ReceiptPath,
    string? ReferencePhotoPath,
    bool IsExpressSlot,
    string TimeAgo
);

public record RealtimeWorkDto(
    int Id,
    string Title,
    string Category,
    string? Description,
    string ImagePath,
    string Action // "created", "deleted"
);

public record RealtimePromotionDto(
    int Id,
    string Title,
    string Description,
    string? BadgeText,
    decimal? DiscountPrice,
    bool IsActive,
    string? ImagePath,
    string Action // "created", "updated", "deleted", "toggled"
);

public interface IRealtimeNotifier
{
    Task NotifyNewAppointmentAsync(RealtimeAppointmentDto appointment);
    Task NotifyAppointmentStatusChangedAsync(int appointmentId, string newStatus, string? reason = null);
    Task NotifyPortfolioChangedAsync(RealtimeWorkDto work);
    Task NotifyPromotionChangedAsync(RealtimePromotionDto promo);
}

public class RealtimeNotifier(IHubContext<StudioHub> hubContext, ILogger<RealtimeNotifier> logger) : IRealtimeNotifier
{
    public async Task NotifyNewAppointmentAsync(RealtimeAppointmentDto appointment)
    {
        try
        {
            // Notificar a todos y en especial al grupo de administradores
            await hubContext.Clients.All.SendAsync("ReceiveNewAppointment", appointment);
            logger.LogInformation("SignalR: Notificada nueva cita #{AppointmentId} de {ClientName} en tiempo real.", appointment.Id, appointment.ClientName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error emitiendo evento SignalR para nueva cita #{AppointmentId}", appointment.Id);
        }
    }

    public async Task NotifyAppointmentStatusChangedAsync(int appointmentId, string newStatus, string? reason = null)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("ReceiveAppointmentStatusChange", new { appointmentId, newStatus, reason });
            logger.LogInformation("SignalR: Cambio de estado de cita #{AppointmentId} a '{Status}'.", appointmentId, newStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error emitiendo evento SignalR de cambio de estado para cita #{AppointmentId}", appointmentId);
        }
    }

    public async Task NotifyPortfolioChangedAsync(RealtimeWorkDto work)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("ReceivePortfolioUpdate", work);
            logger.LogInformation("SignalR: Portafolio actualizado ({Action} #{WorkId} - '{Title}').", work.Action, work.Id, work.Title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error emitiendo evento SignalR de actualización de portafolio.");
        }
    }

    public async Task NotifyPromotionChangedAsync(RealtimePromotionDto promo)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("ReceivePromotionUpdate", promo);
            logger.LogInformation("SignalR: Promoción actualizada ({Action} #{PromoId} - '{Title}').", promo.Action, promo.Id, promo.Title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error emitiendo evento SignalR de actualización de promociones.");
        }
    }
}
