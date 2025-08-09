namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BreakReasonResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = String.Empty;

    public string Description { get; set; } = String.Empty;

    public string Color { get; set; } = String.Empty;

    public int DefaultLength { get; set; } = 0;

    public double DefaultValue { get; set; }

    public bool HideInGantt { get; set; }

    public bool Undeletable { get; set; }

    public Guid Macro { get; set; }
}
