using System.Net;
using System.Net.Mail;

namespace StudioGirlPink.Services.Email;

/// <summary>
/// Proveedor de correo listo para producción vía SMTP (compatible con Mailtrap, SendGrid, Resend, Amazon SES).
/// </summary>
public class SmtpEmailProvider(IConfiguration configuration, ILogger<SmtpEmailProvider> logger) : IEmailProvider
{
    public async Task SendEmailAsync(EmailPayload payload, CancellationToken cancellationToken = default)
    {
        var host = configuration["SMTP_HOST"] ?? configuration["Smtp:Host"];
        var portStr = configuration["SMTP_PORT"] ?? configuration["Smtp:Port"] ?? "587";
        var user = configuration["SMTP_USER"] ?? configuration["Smtp:User"];
        var pass = configuration["SMTP_PASS"] ?? configuration["Smtp:Pass"];
        var fromEmail = configuration["SMTP_FROM"] ?? configuration["Smtp:From"] ?? "no-reply@studiogirlpink.com";
        var fromName = configuration["SMTP_FROM_NAME"] ?? configuration["Smtp:FromName"] ?? "Studio Girl Pink";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            logger.LogWarning("Configuración SMTP incompleta. Revisa las variables de entorno SMTP_*. Se enviará a log.");
            logger.LogInformation("[FALLBACK LOG MAIL] Para: {To} | Asunto: {Subject}", payload.To, payload.Subject);
            return;
        }

        int.TryParse(portStr, out var port);
        if (port <= 0) port = 587;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = port != 25
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = payload.Subject,
            Body = payload.HtmlBody,
            IsBodyHtml = true
        };
        mailMessage.To.Add(payload.To);

        logger.LogInformation("Enviando correo vía SMTP a {To} por host {Host}:{Port}...", payload.To, host, port);
        await client.SendMailAsync(mailMessage, cancellationToken);
        logger.LogInformation("Correo enviado exitosamente a {To}.", payload.To);
    }
}
