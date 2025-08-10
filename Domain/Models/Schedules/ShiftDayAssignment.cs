namespace Klacks.Api.Domain.Models.Schedules;

public class ShiftDayAssignment
{
    public Guid ShiftId { get; set; }

    public DateOnly Date { get; set; }

    public int DayOfWeek { get; set; }

    public string ShiftName { get; set; } = string.Empty;
}