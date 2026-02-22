// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Results;

public class ClientSummary
{
    public int Id { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    public string Company { get; set; } = string.Empty;
    
    public string DisplayName => !string.IsNullOrWhiteSpace(Company) ? Company : FullName;
    
    public GenderEnum? Gender { get; set; }
    
    public string IdNumber { get; set; } = string.Empty;
    
    public DateOnly? DateOfBirth { get; set; }
    
    public string Email { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string ZipCode { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
    
    public bool HasActiveMembership { get; set; }
    
    public bool HasFormerMembership { get; set; }
    
    public bool HasFutureMembership { get; set; }
}