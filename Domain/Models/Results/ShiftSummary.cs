using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Results;

public class ShiftSummary
{
    public int Id { get; set; }
    
    public int ClientId { get; set; }
    
    public string ClientName { get; set; } = string.Empty;
    
    public DateOnly Date { get; set; }
    
    public TimeOnly StartTime { get; set; }
    
    public TimeOnly EndTime { get; set; }
    
    public TimeSpan Duration { get; set; }
    
    public ShiftStatus Status { get; set; }
    
    public ShiftType Type { get; set; }
    
    public ShiftDayType DayType { get; set; }
    
    public string Location { get; set; } = string.Empty;
    
    public string Notes { get; set; } = string.Empty;
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
    
    public int? GroupId { get; set; }
    
    public string GroupName { get; set; } = string.Empty;
    
    public bool HasBreaks { get; set; }
    
    public int BreakCount { get; set; }
    
    public TimeSpan TotalBreakDuration { get; set; }
}