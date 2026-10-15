// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Human-readable context of escalation chains for the intervention list: the names of their groups and the
/// kinds of the findings that approval chains ask about.
/// </summary>
/// <param name="GroupNames">Group name per group id; ids of deleted or unknown groups are absent.</param>
/// <param name="FindingKinds">Trigger kind per condition id; ids of vanished conditions are absent.</param>
/// <param name="ScenarioStatuses">Status per given scenario id; deleted or unknown scenarios are absent.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant.Escalation;

public sealed record EscalationChainDisplayContext(
    IReadOnlyDictionary<Guid, string> GroupNames,
    IReadOnlyDictionary<Guid, string> FindingKinds,
    IReadOnlyDictionary<Guid, AnalyseScenarioStatus> ScenarioStatuses);
