using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class ShiftExpenses : BaseEntity
{
    public Guid ShiftId { get; set; }

    public Shift? Shift { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxable { get; set; }
}
