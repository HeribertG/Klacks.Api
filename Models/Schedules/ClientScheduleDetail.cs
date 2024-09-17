namespace Klacks_api.Models.Schedules;

public class ClientScheduleDetail
{
  public Guid ClientId { get; set; }

  public int CurrentMonth { get; set; }

  public int CurrentYear { get; set; }

  public Guid Id { get; set; }

  public int NeededRows { get; set; } = 3;
}
