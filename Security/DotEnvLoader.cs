namespace StudioGirlPink.Security;

/// <summary>
/// Cargador de variables de entorno estilo .env y validador de configuración al arranque (Fail-Fast).
/// </summary>
public static class DotEnvLoader
{
    public static void Load(string envFilePath)
    {
        if (!File.Exists(envFilePath)) return;

        foreach (var line in File.ReadAllLines(envFilePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0) continue;

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            // Despojar comillas si existieran
            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    public static void ValidateSchema(IConfiguration configuration, IHostEnvironment environment)
    {
        var errors = new List<string>();

        // Esquema de validación estricto para variables requeridas
        var isProduction = environment.IsProduction() || 
                           string.Equals(Environment.GetEnvironmentVariable("NODE_ENV"), "production", StringComparison.OrdinalIgnoreCase);

        var dbUrl = configuration["DATABASE_URL"] ?? configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            errors.Add("DATABASE_URL (o ConnectionStrings:DefaultConnection) es requerida para la persistencia de datos.");
        }

        var jwtSecret = configuration["JWT_SECRET"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            if (isProduction)
            {
                errors.Add("JWT_SECRET es requerida y debe contener al menos 32 caracteres criptográficamente seguros en producción.");
            }
        }

        if (isProduction)
        {
            var frontendUrl = configuration["FRONTEND_URL"];
            if (string.IsNullOrWhiteSpace(frontendUrl) || !Uri.TryCreate(frontendUrl, UriKind.Absolute, out _))
            {
                errors.Add("FRONTEND_URL debe ser una URL absoluta válida en producción (ej. https://studiogirlpink.com).");
            }

            var smtpHost = configuration["SMTP_HOST"];
            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                errors.Add("SMTP_HOST es requerido para el envío de correos en producción.");
            }
        }

        if (errors.Count > 0)
        {
            var message = "Error de validación de entorno de arranque (Fail-Fast):\n - " + string.Join("\n - ", errors);
            throw new InvalidOperationException(message);
        }
    }
}
