namespace StudioGirlPink.Services;

public static class WhatsAppHelper
{
    public static string FormatWhatsAppUrl(string? rawPhone, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return "https://wa.me/525500000000";
        }

        // Extraer únicamente los dígitos numéricos
        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());

        // En México los números son de 10 dígitos locales. WhatsApp internacional requiere prefijo de país 52
        if (digits.Length == 10)
        {
            digits = "52" + digits;
        }
        else if (digits.Length == 11 && digits.StartsWith("1"))
        {
            digits = "52" + digits[1..];
        }

        var baseUrl = $"https://wa.me/{digits}";
        if (!string.IsNullOrWhiteSpace(message))
        {
            baseUrl += $"?text={Uri.EscapeDataString(message)}";
        }

        return baseUrl;
    }
}
