// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// What happened to the runs of one engine in one apply mode. The accept rate deliberately ignores
/// captures that carry no outcome yet: counting a run that is still awaiting its seal as a rejection
/// would make every fresh plan look bad.
/// </summary>
/// <param name="Engine">Engine that produced the runs.</param>
/// <param name="ApplyKind">How the result reached the schedule.</param>
/// <param name="Total">Captured runs in this group.</param>
/// <param name="Accepted">Runs whose period was sealed.</param>
/// <param name="Rejected">Runs the operator declined.</param>
/// <param name="Superseded">Runs a later run replaced.</param>
/// <param name="Expired">Runs that never reached a seal within the fallback window.</param>
/// <param name="Open">Runs still awaiting an outcome.</param>
/// <param name="AcceptRate">Accepted share of the resolved runs; null when nothing is resolved yet.</param>
/// <param name="AvgCorrectionChurn">Mean share of proposed cells corrected afterwards, over measured runs.</param>
/// <param name="AvgEventChurn">Mean share of post-apply events, over measured runs.</param>
/// <param name="ChurnHistogram">Correction churn in ten buckets of 0.1, for spotting a bimodal spread a mean hides.</param>
/// <param name="WarmStartCount">Runs that started from the previous period's plan.</param>
/// <param name="WarmStartAcceptRate">Accept rate of the warm-started runs; null when none are resolved.</param>
/// <param name="ColdStartAcceptRate">Accept rate of the runs without a warm start; null when none are resolved.</param>
public sealed record WizardRunCaptureEngineStatsDto(
    string Engine,
    string ApplyKind,
    int Total,
    int Accepted,
    int Rejected,
    int Superseded,
    int Expired,
    int Open,
    double? AcceptRate,
    double? AvgCorrectionChurn,
    double? AvgEventChurn,
    IReadOnlyList<int> ChurnHistogram,
    int WarmStartCount,
    double? WarmStartAcceptRate,
    double? ColdStartAcceptRate);
