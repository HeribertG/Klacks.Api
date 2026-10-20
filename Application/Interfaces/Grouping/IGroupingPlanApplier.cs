// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.DTOs.Groups;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingPlanApplier
{
    Task<GroupingApplyResult> ApplyAsync(GroupingApplyCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Describes the start of the client memberships ApplyAsync would create for the same command, without writing
    /// anything: persons that already hold the membership are excluded exactly like in ApplyAsync and, in EntryDate mode,
    /// reported when their start lies after their employment entry.
    /// </summary>
    /// <param name="command">The command the apply would run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<GroupMembershipStartInfo> PreviewStartAsync(GroupingApplyCommand command, CancellationToken cancellationToken);
}
