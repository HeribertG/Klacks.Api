using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class BreakReason : BaseEntity
{
    public string Name { get; set; } = String.Empty;
    public string Description { get; set; } = String.Empty;
    public string Color { get; set; } = String.Empty;
    public int DefaultLength { get; set; } = 0;
    public double DefaultValue { get; set; }
    public bool HideInGantt { get; set; }
    public bool Undeletable { get; set; }
    public Guid Macro { get; set; }

}
