using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedCalendarRule : BaseTruncatedResult
{
    public ICollection<CalendarRule> CalendarRules { get; set; } = null!;
}
