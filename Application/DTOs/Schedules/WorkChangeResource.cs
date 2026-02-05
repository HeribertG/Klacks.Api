using Klacks.Api.Domain.Enums;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class WorkChangeResource
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public WorkResource? Work { get; set; }

    public decimal ChangeTime { get; set; }

    public decimal Surcharges { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public WorkChangeType Type { get; set; }

    public Guid? ReplaceClientId { get; set; }

    public ClientResource? ReplaceClient { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool ToInvoice { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public List<WorkChangeClientResult> ClientResults { get; set; } = [];
}
