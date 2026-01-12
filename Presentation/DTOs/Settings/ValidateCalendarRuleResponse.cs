namespace Klacks.Api.Presentation.DTOs.Settings;

public class ValidateCalendarRuleResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public DateOnly? CalculatedDate { get; set; }
    public string? FormattedDate { get; set; }
    public string? DayOfWeek { get; set; }
    public int Year { get; set; }
}
