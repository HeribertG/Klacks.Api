// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Criteria;

public class AbsenceSearchCriteria : BaseCriteria
{
    public string Language { get; set; } = string.Empty;
    
    public DateOnly? StartDate { get; set; }
    
    public DateOnly? EndDate { get; set; }
    
    public int? ClientId { get; set; }
    
    public int? GroupId { get; set; }
    
    public bool IncludeSubGroups { get; set; }
    
    public string? Reason { get; set; }
    
    public bool? IsApproved { get; set; }
    
    public bool? IsPending { get; set; }
}