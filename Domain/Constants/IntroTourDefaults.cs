// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits and event names of the per-account Klacksy intro tour: how many explicit "later" answers end the offer,
/// the reported event that marks an offer as shown, and the bounds of the stored station ids.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class IntroTourDefaults
{
    public const string TableName = "user_intro_tour_states";

    /// <summary>
    /// Event the frontend reports as soon as the offer is visible. It is not stored as a status: it stamps
    /// last_offered_at and turns a legacy offer into legacy_offered.
    /// </summary>
    public const string OfferedEvent = "offered";

    /// <summary>
    /// The explicit "later" answer that reaches this count dismisses the offer (the second "later").
    /// </summary>
    public const int SnoozeLimit = 2;

    public const int MaxCompletedStations = 50;

    public const int MaxStationIdLength = 64;

    public const int StatusMaxLength = 32;
}
