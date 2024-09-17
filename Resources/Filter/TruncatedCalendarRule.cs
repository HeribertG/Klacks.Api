using Klacks_api.Models.Settings;

namespace Klacks_api.Resources.Filter
{
  public class TruncatedCalendarRule : BaseTruncatedResult
  {
    public ICollection<CalendarRule> CalendarRules { get; set; } = null!;
  }
}
