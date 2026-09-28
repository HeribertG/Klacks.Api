// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant.Grounding;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAnswerGroundingRepository
{
    Task AddFindingAsync(AnswerGroundingFinding finding, CancellationToken cancellationToken = default);

    Task IncrementDailyAsync(AnswerGroundingDailyCounter delta, CancellationToken cancellationToken = default);

    Task<int> CountFindingsAsync(Guid agentId, string scopeKey, string primaryClaimKind, int tier, CancellationToken cancellationToken = default);
}
