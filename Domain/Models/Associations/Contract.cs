using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.CalendarSelections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class Contract : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal GuaranteedHoursPerMonth { get; set; }

    public decimal MaximumHoursPerMonth { get; set; }

    public decimal MinimumHoursPerMonth { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    [ForeignKey("CalendarSelection")]
    public Guid? CalendarSelectionId { get; set; }
    
    [JsonIgnore]
    public CalendarSelection? CalendarSelection { get; set; }
}
