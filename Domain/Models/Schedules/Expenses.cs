using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class Expenses : BaseEntity
{
    public Guid WorkId { get; set; }

    public Work? Work { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxable { get; set; }
}
