namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ExpensesResource
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public WorkResource? Work { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxabled { get; set; }
}
