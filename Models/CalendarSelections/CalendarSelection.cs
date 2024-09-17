using Klacks_api.Datas;
using System.ComponentModel.DataAnnotations;


namespace Klacks_api.Models.CalendarSelections;

public class CalendarSelection : BaseEntity
{
  public string Name { get; set; } = string.Empty;

  public List<SelectedCalendar> SelectedCalendars { get; set; } = new();
}
