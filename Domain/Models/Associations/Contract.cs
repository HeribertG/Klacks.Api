using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Scheduling;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class Contract : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal? GuaranteedHours { get; set; }

    public decimal? MaximumHours { get; set; }

    public decimal? MinimumHours { get; set; }

    public decimal? FullTime { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? SaRate { get; set; }

    public decimal? SoRate { get; set; }

    public PaymentInterval PaymentInterval { get; set; } = PaymentInterval.Monthly;

    [Required]
    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    [ForeignKey("CalendarSelection")]
    public Guid? CalendarSelectionId { get; set; }

    [JsonIgnore]
    public CalendarSelection? CalendarSelection { get; set; }

    [ForeignKey("SchedulingRule")]
    public Guid? SchedulingRuleId { get; set; }

    [JsonIgnore]
    public SchedulingRule? SchedulingRule { get; set; }
}
