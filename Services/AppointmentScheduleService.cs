using StudioGirlPink.Models;

namespace StudioGirlPink.Services;

public record ScheduleValidationResult(
    bool IsValid,
    string? ErrorMessage
);

public record CancellationCheckResult(
    bool CanCancel,
    bool IsDepositRefundable,
    string Message
);

/// <summary>
/// Reglas operativas de agenda, márgenes entre clientes y validaciones de horario.
/// </summary>
public class AppointmentScheduleService
{
    // Margen reglamentario de 1 hora y media (90 minutos) entre clientes
    public const int BufferMinutesBetweenClients = 90;

    /// <summary>
    /// Valida si una fecha y hora propuesta cumple con el horario de atención y no se traslapa con citas existentes.
    /// </summary>
    public ScheduleValidationResult ValidateSlot(
        DateTime localDate,
        TimeSpan timeOfDay,
        int durationMinutes,
        DateTime proposedStartUtc,
        IEnumerable<Appointment> existingAppointments,
        bool isAdminOverride = false)
    {
        var dayOfWeek = localDate.DayOfWeek;

        // 1. Reglas de horario comercial según día
        if (dayOfWeek == DayOfWeek.Sunday)
        {
            if (!isAdminOverride)
            {
                return new ScheduleValidationResult(
                    false,
                    "Los domingos la agenda pública permanece cerrada. Solo la administración puede agendar en domingo (hasta las 9:00 pm).");
            }

            if (timeOfDay < new TimeSpan(9, 0, 0) || timeOfDay > new TimeSpan(21, 0, 0))
            {
                return new ScheduleValidationResult(
                    false,
                    "En domingos el horario habilitado por administración es de 9:00 am a 9:00 pm.");
            }
        }
        else if (dayOfWeek == DayOfWeek.Saturday)
        {
            // Sábados de 9:00 am a 3:00 pm (15:00)
            if (timeOfDay < new TimeSpan(9, 0, 0) || timeOfDay > new TimeSpan(15, 0, 0))
            {
                return new ScheduleValidationResult(
                    false,
                    "El horario de atención para los sábados es de 9:00 am a 3:00 pm.");
            }
        }
        else // Lunes a Viernes
        {
            // Lunes a Viernes de 10:00 am a 6:00 pm (18:00)
            if (timeOfDay < new TimeSpan(10, 0, 0) || timeOfDay > new TimeSpan(18, 0, 0))
            {
                return new ScheduleValidationResult(
                    false,
                    "El horario de atención de lunes a viernes es de 10:00 am a 6:00 pm.");
            }
        }

        // 2. Validación de traslapes considerando el margen de 1.5 horas entre clientes
        var proposedEndWithBuffer = proposedStartUtc.AddMinutes(Math.Max(durationMinutes, BufferMinutesBetweenClients));

        foreach (var app in existingAppointments)
        {
            if (app.Status == "RechazadaNoAsistio" || app.Status == "Cancelada")
            {
                continue;
            }

            // Validación Dinámica On-Read: Si es un slot express cuyo TTL ya venció, se ignora y el horario queda libre
            if (app.IsExpressSlot && app.SlotExpiresAt.HasValue && app.SlotExpiresAt.Value < DateTime.UtcNow && app.Status == "PendienteConfirmacion")
            {
                continue;
            }

            var existingStart = app.StartsAt;
            var existingEndWithBuffer = app.StartsAt.AddMinutes(Math.Max(app.DurationMinutes, BufferMinutesBetweenClients));

            if (proposedStartUtc < existingEndWithBuffer && proposedEndWithBuffer > existingStart)
            {
                return new ScheduleValidationResult(
                    false,
                    "El horario seleccionado ya no se encuentra disponible. Recuerda que contamos con un margen de 1 hora y media entre clientes.");
            }
        }

        return new ScheduleValidationResult(true, null);
    }

    /// <summary>
    /// Políticas de cancelación:
    /// Cancelaciones válidas 2 horas antes de tu cita. Si cancelas con menos de 2 horas o no asistes, el anticipo NO es reembolsable.
    /// Tolerancia de 15 minutos al inicio de la cita.
    /// </summary>
    public CancellationCheckResult CheckCancellationPolicy(DateTime appointmentStart, DateTime requestTime)
    {
        var hoursDiff = (appointmentStart - requestTime).TotalHours;

        if (hoursDiff < 0)
        {
            return new CancellationCheckResult(
                false,
                false,
                "La cita ya ha pasado. No es posible cancelar y el anticipo no es reembolsable.");
        }

        if (hoursDiff >= 2)
        {
            return new CancellationCheckResult(
                true,
                true,
                "Cancelación oportuna (al menos 2 horas antes). Tu anticipo es aplicable a reprogramación.");
        }

        return new CancellationCheckResult(
            true,
            false,
            "Cancelación con menos de 2 horas de anticipación. Según las políticas del estudio, el anticipo de $50 MXN no es reembolsable.");
    }
}
