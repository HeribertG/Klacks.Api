using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Domain.Interfaces;

public interface ISkillExecutor
{
    Task<SkillResult> ExecuteAsync(
        SkillInvocation invocation,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillResult>> ExecuteChainAsync(
        IReadOnlyList<SkillInvocation> invocations,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default);
}
