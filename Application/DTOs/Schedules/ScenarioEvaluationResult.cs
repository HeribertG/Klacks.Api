// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// On-demand evaluation of an AnalyseScenario: its rule-compliance (errors/warnings via the same
/// engine as detect_conflicts) plus the change-set it introduces over the real plan (added/removed
/// effective grid entries). Used by the evaluate_scenario skill so Klacksy can recommend whether a
/// proposal is safe to accept — the accept itself stays a manual step. The shape is rankable
/// (Errors, Warnings, AddedWorkEntries) so competing scenarios can later be ordered.
/// </summary>
/// <param name="Found">False when no scenario matched the requested id/token</param>
/// <param name="ScenarioId">Id of the evaluated scenario</param>
/// <param name="Token">Isolation token of the evaluated scenario</param>
/// <param name="Name">Scenario name</param>
/// <param name="Status">Active, Accepted or Rejected</param>
/// <param name="GroupId">Group the scenario belongs to (null = all groups)</param>
/// <param name="FromDate">Scenario period start (ISO yyyy-MM-dd)</param>
/// <param name="UntilDate">Scenario period end (ISO yyyy-MM-dd)</param>
/// <param name="TotalConflicts">Total rule violations in the scenario</param>
/// <param name="Errors">Number of error-severity violations (these block a safe accept)</param>
/// <param name="Warnings">Number of warning-severity violations (rest/overtime)</param>
/// <param name="Info">Number of info-severity findings</param>
/// <param name="RuleCompliant">True when the scenario has zero error-severity violations</param>
/// <param name="ByCode">Violation count grouped by stable code</param>
/// <param name="Conflicts">The violations (capped for payload size)</param>
/// <param name="ConflictsTruncated">True when the conflict list was capped</param>
/// <param name="RealEntryCount">Effective grid entries in the real plan for the period</param>
/// <param name="ScenarioEntryCount">Effective grid entries in the scenario for the period</param>
/// <param name="AddedEntryCount">Entries present in the scenario but not the real plan</param>
/// <param name="RemovedEntryCount">Entries present in the real plan but not the scenario</param>
/// <param name="AddedWorkEntries">Added entries of type Work (propose_plan coverage)</param>
/// <param name="AddedReplacementEntries">Added replacement entries (cover_absence)</param>
/// <param name="AddedBreakEntries">Added break entries (absences)</param>
/// <param name="AddedByType">Added entry count grouped by resolved entry type</param>
/// <param name="AddedEntries">The added entries (capped for payload size)</param>
/// <param name="RemovedEntries">The removed entries (capped for payload size)</param>
/// <param name="ChangesTruncated">True when the added/removed lists were capped</param>
/// <param name="Recommendation">Conservative, advisory accept/reject guidance (never auto-acts)</param>
public sealed record ScenarioEvaluationResult(
    bool Found,
    Guid ScenarioId,
    Guid Token,
    string Name,
    string Status,
    Guid? GroupId,
    string FromDate,
    string UntilDate,
    int TotalConflicts,
    int Errors,
    int Warnings,
    int Info,
    bool RuleCompliant,
    IReadOnlyDictionary<string, int> ByCode,
    IReadOnlyList<ScenarioConflictDto> Conflicts,
    bool ConflictsTruncated,
    int RealEntryCount,
    int ScenarioEntryCount,
    int AddedEntryCount,
    int RemovedEntryCount,
    int AddedWorkEntries,
    int AddedReplacementEntries,
    int AddedBreakEntries,
    IReadOnlyDictionary<string, int> AddedByType,
    IReadOnlyList<ScenarioChangeEntry> AddedEntries,
    IReadOnlyList<ScenarioChangeEntry> RemovedEntries,
    bool ChangesTruncated,
    string Recommendation)
{
    public static ScenarioEvaluationResult NotFound() => new(
        Found: false,
        ScenarioId: Guid.Empty,
        Token: Guid.Empty,
        Name: string.Empty,
        Status: string.Empty,
        GroupId: null,
        FromDate: string.Empty,
        UntilDate: string.Empty,
        TotalConflicts: 0,
        Errors: 0,
        Warnings: 0,
        Info: 0,
        RuleCompliant: false,
        ByCode: new Dictionary<string, int>(),
        Conflicts: [],
        ConflictsTruncated: false,
        RealEntryCount: 0,
        ScenarioEntryCount: 0,
        AddedEntryCount: 0,
        RemovedEntryCount: 0,
        AddedWorkEntries: 0,
        AddedReplacementEntries: 0,
        AddedBreakEntries: 0,
        AddedByType: new Dictionary<string, int>(),
        AddedEntries: [],
        RemovedEntries: [],
        ChangesTruncated: false,
        Recommendation: string.Empty);
}
