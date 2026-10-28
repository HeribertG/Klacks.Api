// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decorator that keeps the skills of PlaygroundBlockedSkills from running on the public Playground. It sits outside the
/// real executor, so a blocked skill is refused before the permission check, the autonomy gate, the usage tracking
/// (which would store the visitor's parameters) and the skill itself; it covers every caller that resolves the executor
/// (chat, REST, plans, confirmations, scheduled tasks). A chain that contains a blocked skill is refused as a whole, one
/// error per invocation, so no earlier step runs. With the Playground mode off every call is forwarded unchanged.
/// </summary>
/// <param name="inner">The real skill executor</param>
/// <param name="mode">Tells whether the instance is the Playground</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Playground;

public sealed class PlaygroundBlockedSkillExecutor : ISkillExecutor
{
    private readonly ISkillExecutor _inner;
    private readonly IPlaygroundMode _mode;

    public PlaygroundBlockedSkillExecutor(ISkillExecutor inner, IPlaygroundMode mode)
    {
        _inner = inner;
        _mode = mode;
    }

    public Task<SkillResult> ExecuteAsync(
        SkillInvocation invocation,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (_mode.IsActive && PlaygroundBlockedSkills.IsBlocked(invocation.SkillName))
        {
            return Task.FromResult(Blocked());
        }

        return _inner.ExecuteAsync(invocation, context, cancellationToken);
    }

    public Task<IReadOnlyList<SkillResult>> ExecuteChainAsync(
        IReadOnlyList<SkillInvocation> invocations,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (_mode.IsActive && invocations.Any(invocation => PlaygroundBlockedSkills.IsBlocked(invocation.SkillName)))
        {
            IReadOnlyList<SkillResult> refused = invocations.Select(_ => Blocked()).ToList();
            return Task.FromResult(refused);
        }

        return _inner.ExecuteChainAsync(invocations, context, cancellationToken);
    }

    private static SkillResult Blocked() => SkillResult.Error(PlaygroundHardeningConstants.FeatureBlockedMessage);
}
