namespace Klacks.Api.Presentation.DTOs.Settings;

public class VatResource
{
    public Guid Id { get; set; }
    public decimal VATRate { get; set; }
    public bool IsDefault { get; set; }
}