namespace Klacks.Api.Application.DTOs.Settings;

public class ValidateCalendarRuleRequest
{
    public string Rule { get; set; } = string.Empty;
    public string? SubRule { get; set; }
    public int? Year { get; set; }
}
