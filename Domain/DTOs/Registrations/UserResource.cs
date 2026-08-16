// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs.Registrations;

public class UserResource
{
    public string Id { get; set; } = string.Empty;
     
    public string UserName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
   
    public string Email { get; set; } = string.Empty;
    
    public bool IsAdmin { get; set; }

    public bool IsAuthorised { get; set; }

    /// <summary>
    /// Set when the account is deliberately deactivated, so the administration list can show the
    /// state instead of only offering the irreversible deletion. Null means the account is active.
    /// </summary>
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>
    /// Free-text phone number for reachability outside the messenger channels (e.g. the escalation
    /// call list). Not validated or format-checked; not wired to any delivery channel yet.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
