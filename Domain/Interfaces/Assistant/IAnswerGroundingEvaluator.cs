// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAnswerGroundingEvaluator
{
    Task EvaluateAsync(
        Guid agentId,
        LLMContext context,
        string responseContent,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        CancellationToken cancellationToken = default);
}
