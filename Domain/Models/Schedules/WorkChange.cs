using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Models.Schedules;

public class WorkChange : BaseEntity
{
    public Guid WorkId { get; set; }

    public Work? Work { get; set; }

    public decimal ChangeTime { get; set; }

    public decimal Surcharges { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public WorkChangeType Type { get; set; }

    public Guid? ReplaceClientId { get; set; }

    public Client? ReplaceClient { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool ToInvoice { get; set; }
}
