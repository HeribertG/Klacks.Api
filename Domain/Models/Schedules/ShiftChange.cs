using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Models.Schedules;

public class ShiftChange : BaseEntity
{
    public Guid WorkId { get; set; }

    public Work? Work { get; set; }

    public decimal ChangeTime { get; set; }

    public ShiftChangeType Type { get; set; }

    public Guid? ReplaceClientId { get; set; }

    public Client? ReplaceClient { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool ToInvoice { get; set; }
}
