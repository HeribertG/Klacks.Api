namespace Klacks.Api.Resources.Schedules
{
  public class BreakResource
  {
    public Guid AbsenceId { get; set; }

    public Guid ClientId { get; set; }

    public DateTime From { get; set; }

    public Guid Id { get; set; }

    public string? Information { get; set; }

    public DateTime Until { get; set; }
  }
}
