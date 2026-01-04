using Klacks.Api.Domain.Enums;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Presentation.DTOs.Associations;

public class ContractResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal GuaranteedHours { get; set; }

    public decimal MaximumHours { get; set; }

    public decimal MinimumHours { get; set; }

    public decimal FullTime { get; set; }

    public decimal NightRate { get; set; }

    public decimal HolidayRate { get; set; }

    public decimal SaRate { get; set; }

    public decimal SoRate { get; set; }

    public PaymentInterval PaymentInterval { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public CalendarSelectionResource? CalendarSelection { get; set; }

    public Guid? CalendarSelectionId { get; set; }
}