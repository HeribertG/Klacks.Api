// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRelationLearner
{
    Task<int> LearnAsync(CancellationToken cancellationToken = default);
}
