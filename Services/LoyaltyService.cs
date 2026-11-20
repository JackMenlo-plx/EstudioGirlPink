namespace StudioGirlPink.Services;

public record LoyaltyBenefit(
    int CompletedVisits,
    int NextRewardAt,
    int RemainingVisitsForReward,
    bool IsRewardEligible,
    string RewardMessage
);

/// <summary>
/// Módulo de Fidelización: Tarjeta de Lealtad (5 servicios = 6to con Gel gratis o 50% desc en uñas/laminado).
/// </summary>
public class LoyaltyService
{
    public const int VisitsRequiredForReward = 5;

    public LoyaltyBenefit CheckLoyaltyStatus(int completedVisits)
    {
        var currentCycle = completedVisits % (VisitsRequiredForReward + 1);

        if (currentCycle == VisitsRequiredForReward)
        {
            return new LoyaltyBenefit(
                completedVisits,
                completedVisits + 1,
                0,
                true,
                "¡Felicidades! Has alcanzado tu recompensa de lealtad: ¡6to servicio con Gel Semipermanente GRATIS o 50% de descuento en Uñas o Laminado de Ceja!"
            );
        }

        var remaining = VisitsRequiredForReward - currentCycle;
        return new LoyaltyBenefit(
            completedVisits,
            completedVisits + remaining,
            remaining,
            false,
            $"Tienes {completedVisits} citas completadas. Te faltan {remaining} cita(s) para tu regalo de lealtad en tu 6ta visita."
        );
    }
}
