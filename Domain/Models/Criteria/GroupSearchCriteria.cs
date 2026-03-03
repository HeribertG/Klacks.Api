// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Criteria;

public class GroupSearchCriteria : BaseCriteria
{
    public string SearchString { get; set; } = string.Empty;
    
    public bool ActiveDateRange { get; set; }
    
    public bool FormerDateRange { get; set; }
    
    public bool FutureDateRange { get; set; }
    
    public DateOnly? ValidFromDate { get; set; }
    
    public DateOnly? ValidToDate { get; set; }
    
    public int? ParentGroupId { get; set; }
    
    public bool IncludeSubGroups { get; set; }
    
    public int? MaxDepth { get; set; }
    
    public bool? HasMembers { get; set; }
    
    public int? MinMemberCount { get; set; }
    
    public int? MaxMemberCount { get; set; }
}