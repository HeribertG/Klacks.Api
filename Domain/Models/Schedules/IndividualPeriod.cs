using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class IndividualPeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Period> Periods { get; set; } = [];
}
