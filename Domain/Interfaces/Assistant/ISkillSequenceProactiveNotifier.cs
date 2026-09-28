// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillSequenceProactiveNotifier
{
    Task NotifyAfterSkillAsync(string justExecutedSkill, Guid userId, CancellationToken cancellationToken = default);
}
