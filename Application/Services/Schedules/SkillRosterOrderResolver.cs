// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default ISkillRosterOrderResolver. An external agent (MCP, ExternalAgentAccessMode set, also on the scheduled tasks
/// it authored and on the first run of a plan it started) and an unattended self-reflection goal plan (SessionId with
/// GoalSelfReflectionAuditConstants.SessionIdPrefix) have no human recipient and always get the automatic order. Every
/// other call acts for the user in the context, whose saved order for the group applies through SavedRosterOrder.
/// </summary>
/// <param name="sortPreferenceRepository">Reads the user's saved order for the group</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class SkillRosterOrderResolver : ISkillRosterOrderResolver
{
    private readonly IClientSortPreferenceRepository _sortPreferenceRepository;

    public SkillRosterOrderResolver(IClientSortPreferenceRepository sortPreferenceRepository)
    {
        _sortPreferenceRepository = sortPreferenceRepository;
    }

    public async Task<(IReadOnlyList<Guid> AgentIds, bool UserDefined)> ResolveAsync(
        SkillExecutionContext context,
        Guid groupId,
        IReadOnlyList<Guid> agentIds,
        CancellationToken cancellationToken)
    {
        if (!ActsForHuman(context))
        {
            return (agentIds, false);
        }

        var savedOrder = await _sortPreferenceRepository.GetByUserAndGroupAsync(
            context.UserId.ToString(), groupId, cancellationToken);
        return SavedRosterOrder.Apply(agentIds, savedOrder);
    }

    private static bool ActsForHuman(SkillExecutionContext context) =>
        !context.ExternalAgentAccessMode.HasValue
        && context.SessionId?.StartsWith(GoalSelfReflectionAuditConstants.SessionIdPrefix, StringComparison.Ordinal) != true;
}
