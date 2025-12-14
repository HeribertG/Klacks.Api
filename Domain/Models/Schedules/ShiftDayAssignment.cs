namespace Klacks.Api.Domain.Models.Schedules;

public class ShiftDayAssignment
{
    public Guid ShiftId { get; set; }

    public DateOnly Date { get; set; }

    public int DayOfWeek { get; set; }

    public string ShiftName { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public TimeOnly StartShift { get; set; }

    public TimeOnly EndShift { get; set; }

    public decimal WorkTime { get; set; }

    public bool IsSporadic { get; set; }

    public bool IsTimeRange { get; set; }

    public int ShiftType { get; set; }

    public int Status { get; set; }

    public bool IsInTemplateContainer { get; set; }

    public int SumEmployees { get; set; }

    public int Quantity { get; set; }

    public int SporadicScope { get; set; }

    public int Engaged { get; set; }
}