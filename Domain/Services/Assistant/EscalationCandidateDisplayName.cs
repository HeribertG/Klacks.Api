// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The one spelling of a roster member's display name that is frozen into EscalationStage.UserDisplayName,
/// shared by the absence roster (EscalationRosterService) and the condition approval roster so a stage
/// reads the same whichever path produced it.
/// </summary>
/// <param name="user">The account whose name is rendered.</param>

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Services.Assistant;

public static class EscalationCandidateDisplayName
{
    public static string Build(AppUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        var userName = user.UserName ?? user.Id;
        return name.Length > 0 ? $"{name} ({userName})" : userName;
    }
}
