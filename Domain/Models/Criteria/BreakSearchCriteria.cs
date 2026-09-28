// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Criteria;

public class BreakSearchCriteria : BaseCriteria
{
    public DateOnly? StartDate { get; set; }
    
    public DateOnly? EndDate { get; set; }
    
    public int? ClientId { get; set; }
    
    public int? GroupId { get; set; }
    
    public bool IncludeSubGroups { get; set; }
    
    public string? Reason { get; set; }
    
    public bool? IsPaid { get; set; }
    
    public int? MembershipYear { get; set; }
}