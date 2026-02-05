namespace Klacks.Api.Application.DTOs.Filter;

public class CalendarRulesFilter : BaseFilter
{
    public string Language { get; set; } = string.Empty;
    
    public List<StateCountryToken> List { get; set; } = new List<StateCountryToken>();
}
