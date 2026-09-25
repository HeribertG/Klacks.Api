// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Optional hook of the autonomy gate: computes, on the server, what a held sensitive skill call would do, before the
/// confirmation token is issued. Implementations must only read; the parameters are the stored ones the confirmed call
/// will replay and must not be modified.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillConfirmationPreviewProvider
{
    bool Supports(string skillName);

    Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
