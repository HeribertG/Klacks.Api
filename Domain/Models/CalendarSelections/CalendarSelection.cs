using Klacks.Api.Datas;
using System.ComponentModel.DataAnnotations;


namespace Klacks.Api.Domain.Models.CalendarSelections;

public class CalendarSelection : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public List<SelectedCalendar> SelectedCalendars { get; set; } = new();
}
