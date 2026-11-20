namespace StudioGirlPink.Services.Email;

public record EmailPayload(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null
);

/// <summary>
/// Interfaz desacoplada para envío de correos (Strategy Pattern).
/// Permite alternar proveedores según el entorno (DevConsole, SMTP, SendGrid, Resend) sin acoplar la capa de negocio.
/// </summary>
public interface IEmailProvider
{
    Task SendEmailAsync(EmailPayload payload, CancellationToken cancellationToken = default);
}
