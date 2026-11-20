namespace StudioGirlPink.ViewModels;

public class AppointmentConfirmationViewModel
{
    public int AppointmentId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public decimal QuotedPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public string DepositStatus { get; set; } = "PendienteRevision";
    public string Status { get; set; } = "PendienteConfirmacion";
    public bool HasReceipt { get; set; }
    public string PriceBreakdown { get; set; } = string.Empty;
}
