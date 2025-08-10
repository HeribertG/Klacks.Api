using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using Newtonsoft.Json;

namespace Klacks.Api.Domain.Models.Schedules;

public class Work : BaseEntity
{
    public virtual Client Client { get; set; } = null!;

    public Guid ClientId { get; set; }

    public DateTime From { get; set; }

    public string? Information { get; set; }

    public bool IsSealed { get; set; }

    public virtual Shift? Shift { get; set; } = null!;

    public Guid ShiftId { get; set; }

    public DateTime Until { get; set; }
}
