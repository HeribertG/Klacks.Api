using Klacks.Api.Models.Settings;

namespace Klacks.Api.Presentation.Resources.Filter
{
    public class TruncatedCalendarRule : BaseTruncatedResult
    {
        public ICollection<CalendarRule> CalendarRules { get; set; } = null!;
    }
}
