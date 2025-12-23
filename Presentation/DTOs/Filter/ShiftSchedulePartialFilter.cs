namespace Klacks.Api.Presentation.DTOs.Filter;

public class ShiftSchedulePartialFilter
{
    public List<ShiftDatePairFilter> ShiftDatePairs { get; set; } = [];
}

public class ShiftDatePairFilter
{
    public Guid ShiftId { get; set; }

    public DateTime Date { get; set; }
}
