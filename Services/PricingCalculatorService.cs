namespace StudioGirlPink.Services;

public record PriceCalculationResult(
    decimal BasePrice,
    decimal QuotedPrice,
    decimal DepositAmount,
    int EstimatedMinutes,
    string DetailsBreakdown
);

/// <summary>
/// Motor de cálculo dinámico de precios conforme a la sección 2 del Catálogo de Servicios.
/// </summary>
public class PricingCalculatorService
{
    public const decimal MandatoryDeposit = 50.00m;

    public PriceCalculationResult Calculate(
        string serviceName,
        int? nailLevel = null,
        string? hairLength = null,
        bool hasExtraVolume = false)
    {
        var normName = (serviceName ?? string.Empty).ToLowerInvariant().Trim();

        // 1. Uñas Acrílicas: Base $200 (Nivel 1) + $50 por cada nivel adicional (1 al 10)
        if (normName.Contains("uñas acrílicas") || normName.Contains("unas acrilicas") || normName.Contains("acrílicas"))
        {
            var level = Math.Clamp(nailLevel ?? 1, 1, 10);
            var additional = (level - 1) * 50m;
            var total = 200m + additional;
            var minutes = 60 + (level * 6); // 40m - 120m aprox
            var breakdown = $"Uñas Acrílicas Nivel {level}: Base $200 + {level - 1} niveles extra (${additional}) = ${total} MXN";
            return new PriceCalculationResult(200m, total, MandatoryDeposit, Math.Min(minutes, 120), breakdown);
        }

        // 7. Alisado Progresivo: Al hombro $500 | Medio $900 | A la cintura $1,350 | Extra-largo $1,500 | +$200 volumen
        if (normName.Contains("alisado"))
        {
            decimal hairBase = 500m;
            var lengthLabel = "Al hombro";
            var minutes = 120;

            switch ((hairLength ?? "Hombro").ToLowerInvariant())
            {
                case "medio":
                case "cabellomedio":
                    hairBase = 900m;
                    lengthLabel = "Medio (hombro a media espalda)";
                    minutes = 150;
                    break;
                case "cintura":
                case "alacintura":
                    hairBase = 1350m;
                    lengthLabel = "A la cintura";
                    minutes = 200;
                    break;
                case "extralargo":
                case "extra-largo":
                    hairBase = 1500m;
                    lengthLabel = "Extra-largo";
                    minutes = 240;
                    break;
                default:
                    hairBase = 500m;
                    lengthLabel = "Al hombro";
                    minutes = 120;
                    break;
            }

            var volumeAdd = hasExtraVolume ? 200m : 0m;
            var total = hairBase + volumeAdd;
            var breakdown = $"Alisado Progresivo ({lengthLabel}): ${hairBase} MXN" +
                            (hasExtraVolume ? " + $200 MXN por volumen extra" : "") +
                            $" = ${total} MXN";

            return new PriceCalculationResult(500m, total, MandatoryDeposit, minutes, breakdown);
        }

        // 2. Baño de Acrílico: Desde $200 (40 min - 1 hr)
        if (normName.Contains("baño de acrílico") || normName.Contains("bano de acrilico"))
        {
            return new PriceCalculationResult(200m, 200m, MandatoryDeposit, 60, "Baño de Acrílico sobre uña natural: $200 MXN");
        }

        // 3. Gel Semipermanente: Desde $150 (40 min - 1 hr)
        if (normName.Contains("gel semipermanente") || normName.Contains("semipermanente"))
        {
            return new PriceCalculationResult(150m, 150m, MandatoryDeposit, 60, "Gel Semipermanente: $150 MXN");
        }

        // 4. Ruber (Nivelación): Desde $220 (40 min - 1 hr)
        if (normName.Contains("ruber") || normName.Contains("rubber"))
        {
            return new PriceCalculationResult(220m, 220m, MandatoryDeposit, 60, "Ruber Nivelación: $220 MXN");
        }

        // 5. Acripie: Desde $250 (40 min - 1 hr)
        if (normName.Contains("acripie"))
        {
            return new PriceCalculationResult(250m, 250m, MandatoryDeposit, 60, "Acripie estético profesional: $250 MXN");
        }

        // 6. Laminado de Ceja: $200 (30 min)
        if (normName.Contains("laminado"))
        {
            return new PriceCalculationResult(200m, 200m, MandatoryDeposit, 30, "Laminado de Ceja precio fijo: $200 MXN");
        }

        // 8. Efectos de Color con Extensión: Desde $450 (30 min - 1 hr)
        if (normName.Contains("efectos de color") || normName.Contains("extension"))
        {
            return new PriceCalculationResult(450m, 450m, MandatoryDeposit, 60, "Efectos de Color con Extensión: $450 MXN");
        }

        // 9. Servicios Especiales / Personalizados
        return new PriceCalculationResult(250m, 250m, MandatoryDeposit, 90, "Servicio Personalizado / Cotización sujeta a aprobación con foto de referencia.");
    }
}
