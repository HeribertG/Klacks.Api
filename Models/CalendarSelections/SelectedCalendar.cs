using Klacks_api.Datas;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks_api.Models.CalendarSelections
{
  public class SelectedCalendar : BaseEntity
  {
    [JsonIgnore]
    public CalendarSelection? CalendarSelection { get; set; }

    [Required]
    [ForeignKey("CalendarSelections")]
    public Guid CalendarSelectionId { get; set; }

    public string Country { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;
  }
}
