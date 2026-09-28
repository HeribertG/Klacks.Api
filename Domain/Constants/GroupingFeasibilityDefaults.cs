// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Tunables of the grouping feasibility analysis: the default and maximum analysis horizon, the length of
/// the fingerprint prefix shown in chat, the placeholder standing for a group that the plan would create,
/// the per-code list cap of the chat report, the cap of listed planning units and how long a plan preview unlocks apply=true.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GroupingFeasibilityDefaults
{
    public const int DefaultHorizonDays = 56;
    public const int MaxHorizonDays = 366;
    public const int FingerprintDisplayLength = 12;
    public const int MaxListedItemsPerCode = 25;
    public const int MaxListedPlanningUnits = 10;
    public const string NewGroupKey = "new-group";
    public const int MinutesPerDay = 1440;
    public const int MinutesPerHour = 60;
    public const int DaysPerWeek = 7;
    public const double EarthRadiusKilometers = 6371.0;
    public const int DailySnapshotRetentionDays = 2;
    public const string DailySnapshotCacheKeyPrefix = "grouping-feasibility-day:";
    public const int PreviewValidityMinutes = 30;
    public const string PreviewCacheKeyPrefix = "grouping-plan-preview:";

    public static readonly Guid NewGroupPlaceholderId = new("6e657767-7270-4000-8000-000000000001");
}
