// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Criteria;

public class ClientSearchCriteria : BaseCriteria
{
    public string SearchString { get; set; } = string.Empty;
    
    public GenderEnum? Gender { get; set; }
    
    public AddressTypeEnum? AddressType { get; set; }
    
    public string? StateOrCountryCode { get; set; }
    
    public string? AnnotationText { get; set; }
    
    public bool? HasAnnotation { get; set; }
    
    public bool? IsActiveMember { get; set; }
    
    public bool? IsFormerMember { get; set; }
    
    public bool? IsFutureMember { get; set; }
    
    public DateOnly? MembershipStartDate { get; set; }
    
    public DateOnly? MembershipEndDate { get; set; }
    
    public int? MembershipYear { get; set; }
    
    public int? BreaksYear { get; set; }
}