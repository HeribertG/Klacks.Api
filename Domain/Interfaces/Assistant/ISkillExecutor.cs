// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

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
