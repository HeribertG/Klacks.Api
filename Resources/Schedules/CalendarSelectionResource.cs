using Klacks_api.Models.Schedules;

namespace Klacks_api.Resources.Schedules
{
  public class CalendarSelectionResource
  {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<SelectedCalendarResource> SelectedCalendars { get; set; } = new();
  }
}
