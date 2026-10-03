// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits, defaults and ParametersJson property names of the planning-constraint model.
/// ProposalLifetimeDays is the owner decision of 2026-10-03: a Proposed row nobody decided on expires
/// (to Rejected) after 30 days.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PlanningConstraintDefaults
{
    public const int ProposalLifetimeDays = 30;

    public const int CurrentParametersSchemaVersion = 1;

    public const int MinimumSupportedParametersSchemaVersion = 1;

    /// <summary>
    /// Upper bound of a constraint weight. A soft penalty is Weight times the excess and is summed with other
    /// soft terms of comparable scale (CounterRule soft weight 1.0); a thousandfold weight already lets one rule
    /// outvote every other soft goal, so anything above turns a soft rule into a hidden hard one - that must be
    /// expressed as Severity Hard instead.
    /// </summary>
    public const double MaxWeight = 1000d;

    public const double DefaultCounterRuleSoftWeight = 1.0;

    public const int SourceTextMaxLength = 4000;

    public const int ParaphraseMaxLength = 2000;

    public const int ActorMaxLength = 256;

    public const int ImportSourceKeyMaxLength = 200;

    public const int ImportContentHashMaxLength = 64;

    public const int MaxRunLimit = 366;

    public const int MaxDayDistanceLimit = 366;

    public const string ExpirySweepActor = "system:planning-constraint-expiry";

    public const string SchemaVersionProperty = "schemaVersion";

    public const string KindProperty = "kind";

    public const string MaxRunProperty = "maxRun";

    public const string FromProperty = "from";

    public const string ToProperty = "to";

    public const string WithinDaysProperty = "withinDays";

    public const string FreeDaysProperty = "freeDays";

    public const string MetricProperty = "metric";

    public const string WindowProperty = "window";

    public const string MaxSpreadProperty = "maxSpread";

    public const string ProRataProperty = "proRata";

    public const string WeekendDaysProperty = "weekendDays";

    public const bool DefaultProRata = true;

    /// <summary>
    /// Owner decision 7 (2026-10-04): a segment is a night for the sequence and fairness rules only when it overlaps
    /// the night window by more than an hour (DE ArbZG and TVoeD use two hours, NL more than one hour); an early shift
    /// from 05:00 against a 23:00-06:00 window is therefore no night.
    /// </summary>
    public const int DefaultNightRuleMinOverlapMinutes = 60;
}
