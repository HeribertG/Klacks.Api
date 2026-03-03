// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillUsageRecord>> GetUsageAsync(
        DateTime from,
        CancellationToken cancellationToken = default);
}
