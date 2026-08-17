// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class EscalationRosterMemberResource
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool HasPhoneNumber { get; set; }

    public bool IsCurrentlyAbsent { get; set; }
}
