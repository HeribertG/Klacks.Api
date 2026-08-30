// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillUsageTracker
{
    Task TrackAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        SkillResult result,
        TimeSpan duration,
        CancellationToken cancellationToken = default,
        Guid? recordId = null);

    /// <summary>
    /// Persists a failure that happened before the skill was dispatched (hallucinated name, missing
    /// permission, invalid parameter, autonomy-gate hold, missing UI context, exception), so the
    /// failure classes of W1.2 are countable per SQL instead of only visible in logs.
    /// </summary>
    Task TrackFailureAsync(
        string skillName,
        SkillFailureKind failureKind,
        SkillExecutionContext context,
        Dictionary<string, object>? parameters,
        string? errorMessage,
        TimeSpan duration,
        SkillCategory category = SkillCategory.Action,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillUsageRecord>> GetUsageAsync(
        DateTime from,
        CancellationToken cancellationToken = default);
}
