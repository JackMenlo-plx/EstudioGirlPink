using Microsoft.AspNetCore.SignalR;

namespace StudioGirlPink.Hubs;

/// <summary>
/// Hub central de SignalR para actualizaciones bidireccionales en tiempo real.
/// Notifica citas entrantes al administrador y sincroniza cambios de portafolio y promociones para los clientes.
/// </summary>
public class StudioHub : Hub
{
    public const string AdminGroup = "Administrators";
    public const string PublicGroup = "PublicClients";

    public async Task JoinAdminGroup()
    {
        if (Context.User?.IsInRole("Administrator") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }
    }

    public async Task JoinPublicGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, PublicGroup);
    }

    public override async Task OnConnectedAsync()
    {
        // Por defecto, cualquier visitante se une al canal público
        await Groups.AddToGroupAsync(Context.ConnectionId, PublicGroup);
        if (Context.User?.IsInRole("Administrator") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }
        await base.OnConnectedAsync();
    }
}
