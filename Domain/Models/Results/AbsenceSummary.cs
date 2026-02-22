namespace Klacks.Api.Domain.Models.Results;

public class AbsenceSummary
{
    public int Id { get; set; }
    
    public int ClientId { get; set; }
    
    public string ClientName { get; set; } = string.Empty;
    
    public DateOnly StartDate { get; set; }
    
    public DateOnly EndDate { get; set; }
    
    public int DurationDays { get; set; }
    
    public string Reason { get; set; } = string.Empty;
    
    public bool IsApproved { get; set; }
    
    public bool IsPending { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
    
    public int? GroupId { get; set; }
    
    public string GroupName { get; set; } = string.Empty;
}