namespace StudioGirlPink.Services.Email;

/// <summary>
/// Proveedor de correo para entornos de desarrollo / pruebas.
/// Registra en logs estructurados con formato claro para inspección de tokens sin requerir servicios externos.
/// </summary>
public class DevConsoleEmailProvider(ILogger<DevConsoleEmailProvider> logger) : IEmailProvider
{
    public Task SendEmailAsync(EmailPayload payload, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("=================================================");
        logger.LogInformation("📧 [DEV MAILER - MOCK/CONSOLE DISPATCH]");
        logger.LogInformation("Para: {To}", payload.To);
        logger.LogInformation("Asunto: {Subject}", payload.Subject);
        logger.LogInformation("Cuerpo HTML:\n{Body}", payload.HtmlBody);
        logger.LogInformation("=================================================");

        return Task.CompletedTask;
    }
}
