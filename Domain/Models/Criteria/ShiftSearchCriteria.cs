// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Criteria;

public class ShiftSearchCriteria : BaseCriteria
{
    public DateOnly? StartDate { get; set; }
    
    public DateOnly? EndDate { get; set; }
    
    public int? ClientId { get; set; }
    
    public int? GroupId { get; set; }
    
    public bool IncludeSubGroups { get; set; }
    
    public ShiftStatus? Status { get; set; }
    
    public ShiftType? Type { get; set; }
    
    public ShiftDayType? DayType { get; set; }
    
    public DayOfWeek? DayOfWeek { get; set; }
    
    public TimeOnly? StartTime { get; set; }
    
    public TimeOnly? EndTime { get; set; }
    
    public string? Location { get; set; }
}