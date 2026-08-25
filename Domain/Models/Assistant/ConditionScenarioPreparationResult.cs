// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of preparing a remediation scenario for one condition-ledger row.
/// </summary>
/// <param name="Outcome">Which of the four terminal shapes this attempt took.</param>
/// <param name="ScenarioId">The AnalyseScenario now linked to the row, for Prepared and AlreadyPrepared. Null otherwise.</param>
/// <param name="ScenarioToken">Token tagging the cloned schedule rows of a freshly created scenario. Null unless Outcome is Prepared - AlreadyPrepared reports the id it read off the ledger row, which does not carry the token.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConditionScenarioPreparationResult(
    ConditionScenarioPreparationOutcome Outcome,
    Guid? ScenarioId,
    Guid? ScenarioToken)
{
    public bool Prepared => Outcome == ConditionScenarioPreparationOutcome.Prepared;
}
