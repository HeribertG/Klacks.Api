using System.Text.Json.Serialization;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Models.Schedules;

public class DayApproval : BaseEntity
{
    public DateOnly ApprovalDate { get; set; }

    public Guid GroupId { get; set; }

    [JsonIgnore]
    public virtual Group? Group { get; set; }

    public string ApprovedBy { get; set; } = string.Empty;

    public DateTime ApprovedAt { get; set; }
}
