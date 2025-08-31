using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Presentation.DTOs.Associations;

public class ContractResource
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public decimal GuaranteedHoursPerMonth { get; set; }

    public decimal MaximumHoursPerMonth { get; set; }

    public decimal MinimumHoursPerMonth { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public CalendarSelectionResource? CalendarSelection { get; set; }

    public Guid? CalendarSelectionId { get; set; }
}