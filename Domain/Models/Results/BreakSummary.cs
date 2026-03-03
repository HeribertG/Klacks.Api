// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Results;

public class BreakSummary
{
    public int Id { get; set; }
    
    public int ClientId { get; set; }
    
    public string ClientName { get; set; } = string.Empty;
    
    public DateOnly StartDate { get; set; }
    
    public DateOnly EndDate { get; set; }
    
    public int DurationDays { get; set; }
    
    public string Reason { get; set; } = string.Empty;
    
    public bool IsPaid { get; set; }
    
    public int MembershipYear { get; set; }
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
    
    public int? GroupId { get; set; }
    
    public string GroupName { get; set; } = string.Empty;
}