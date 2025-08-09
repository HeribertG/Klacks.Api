using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Presentation.Resources.Schedules
{
    public class CalendarSelectionResource
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SelectedCalendarResource> SelectedCalendars { get; set; } = new();
    }
}
