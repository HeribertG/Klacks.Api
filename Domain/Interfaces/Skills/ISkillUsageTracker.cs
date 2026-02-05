using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Domain.Interfaces.Skills;

public interface ISkillUsageTracker
{
    Task TrackAsync(
        ISkill skill,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        SkillResult result,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillUsageRecord>> GetUsageAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
