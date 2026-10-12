// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.TurnIntent;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Turn-intent contribution to the toolset: on a confident Ask in an enabled locale, the best-ranked retrieved
/// Explain skills and, when configured, the veto on the detector's forced tool call. Contributes nothing unless the
/// classifier runs in Active mode; Shadow only logs. Start begins the classification before retrieval and returns
/// null when the turn will not be classified; ResolveAsync awaits that started classification or classifies itself.
/// </summary>
public interface ITurnIntentExplainGuarantee
{
    Task<TurnIntentPrediction?>? Start(
        string userMessage, string? language, IReadOnlyCollection<string>? pinnedSkillNames, CancellationToken cancellationToken);

    Task<TurnIntentGuaranteeOutcome> ResolveAsync(SkillToolsetGuaranteeRequest request, CancellationToken cancellationToken);
}
