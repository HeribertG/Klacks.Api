// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant.Grounding;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAnswerGroundingRepository
{
    Task AddFindingAsync(AnswerGroundingFinding finding, CancellationToken cancellationToken = default);

    Task IncrementDailyAsync(AnswerGroundingDailyCounter delta, CancellationToken cancellationToken = default);
}
