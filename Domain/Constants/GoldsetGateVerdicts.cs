// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Verdicts of the paired goldset gate, stored in gate_metrics_json and read by the export script.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class GoldsetGateVerdicts
{
    public const string Passed = "passed";
    public const string BlockedRegression = "blocked_regression";
    public const string NoNetGain = "no_net_gain";
    public const string NotMeasured = "not_measured";
    public const string GoldenCaseRegression = "golden_case_regression";
}
