// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillSequenceProactiveNotifier
{
    Task NotifyAfterSkillAsync(string justExecutedSkill, CancellationToken cancellationToken = default);
}
