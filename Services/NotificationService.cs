namespace StudioGirlPink.Services;

public interface INotificationService
{
    Task SendStatusNotificationAsync(string phoneNumber, string clientName, int appointmentId, string status, string? reason = null, DateTime? appointmentDate = null, string? serviceName = null);
    string GenerateWhatsAppDirectLink(string phoneNumber, string clientName, int appointmentId, string status, string? reason = null, DateTime? appointmentDate = null, string? serviceName = null);

    Task SendEmailAsync(string email, string subject, string htmlMessage);

}

public class NotificationService(ILogger<NotificationService> logger) : INotificationService
{

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // En desarrollo local registra el enlace generado en la consola.
        logger.LogInformation("=================================================");
        logger.LogInformation("📧 [CORREO ELECTRÓNICO ENVIADO]");
        logger.LogInformation("Para: {Email}", email);
        logger.LogInformation("Asunto: {Subject}", subject);
        logger.LogInformation("Contenido HTML:\n{Message}", htmlMessage);
        logger.LogInformation("=================================================");

        return Task.CompletedTask;
    }
    public Task SendStatusNotificationAsync(
        string phoneNumber,
        string clientName,
        int appointmentId,
        string status,
        string? reason = null,
        DateTime? appointmentDate = null,
        string? serviceName = null)
    {
        var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
        var message = BuildMessageText(clientName, appointmentId, status, reason, appointmentDate, serviceName);

        // Registro estructurado en log / consola (simulación de gateway SMS/WhatsApp de alta fidelidad)
        logger.LogInformation("=================================================");
        logger.LogInformation("📱 [NOTIFICACIÓN AUTOMÁTICA VÍA WHATSAPP / SMS]");
        logger.LogInformation("Destinatario: +52 {Phone} ({Client})", cleanPhone, clientName);
        logger.LogInformation("Mensaje:\n{Message}", message);
        logger.LogInformation("=================================================");

        return Task.CompletedTask;
    }

    public string GenerateWhatsAppDirectLink(
        string phoneNumber,
        string clientName,
        int appointmentId,
        string status,
        string? reason = null,
        DateTime? appointmentDate = null,
        string? serviceName = null)
    {
        var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (cleanPhone.Length == 10)
        {
            cleanPhone = "52" + cleanPhone; // Código de país México por defecto
        }

        var message = BuildMessageText(clientName, appointmentId, status, reason, appointmentDate, serviceName);
        var encoded = Uri.EscapeDataString(message);
        return $"https://wa.me/{cleanPhone}?text={encoded}";
    }

    private static string BuildMessageText(
        string clientName,
        int appointmentId,
        string status,
        string? reason,
        DateTime? appointmentDate,
        string? serviceName)
    {
        var fechaStr = appointmentDate.HasValue 
            ? appointmentDate.Value.ToMexicoTime().ToString("dd/MM/yyyy hh:mm tt") 
            : "la fecha reservada";
        
        var servicioStr = string.IsNullOrWhiteSpace(serviceName) ? "Servicio" : serviceName;

        if (status == "Confirmada")
        {
            return $"¡Hola {clientName}! 🌸 Te escribimos de Studio Girl Pink para confirmarte que tu cita con Folio #{appointmentId} para *{servicioStr}* el día *{fechaStr}* ha sido CONFIRMADA con éxito. ✅ Tu anticipo de $50 MXN fue validado.\n\n" +
                   "📍 Ubicación: Av Alfredo del Mazo Vélez 47b-MZ 026, Pueblosan Miguel Chalma, Tlalnepantla.\n" +
                   "⏰ Tolerancia: 15 minutos.\n" +
                   "👥 Recuerda que por comodidad e higiene no se permiten acompañantes.\n" +
                   "✨ ¡Te esperamos con mucho gusto!";
        }

        if (status == "Rechazada")
        {
            var motivo = string.IsNullOrWhiteSpace(reason) 
                ? "No fue posible validar tu comprobante o no hay disponibilidad en ese horario." 
                : reason;

            return $"Hola {clientName}, te escribimos de Studio Girl Pink. Te informamos que tu solicitud de cita con Folio #{appointmentId} para *{servicioStr}* no pudo ser aprobada.\n\n" +
                   $"⚠️ Motivo: {motivo}\n\n" +
                   "Puedes consultar tu cita en la página web o responder a este mensaje para reprogramar o aclarar cualquier duda. ¡Gracias!";
        }

        if (status == "NuevaSolicitud")
        {
            return $"¡Hola {clientName}! 🌸 Recibimos tu solicitud de cita #{appointmentId} para *{servicioStr}* el día *{fechaStr}* con tu comprobante de anticipo ($50 MXN). En breve la administración revisará tu comprobante y recibirás la confirmación final.";
        }

        return $"Hola {clientName}! Tu cita #{appointmentId} en Studio Girl Pink se encuentra en estatus: {status}.";
    }
    
}
