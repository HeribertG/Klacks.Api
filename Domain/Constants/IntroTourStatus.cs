// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stored lifecycle states of the per-account Klacksy intro tour (table user_intro_tour_states). An account without a
/// row is treated as <see cref="Pending"/>; accounts that existed when the feature shipped start as
/// <see cref="LegacyOffer"/> and get exactly one offer.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class IntroTourStatus
{
    public const string Pending = "pending";
    public const string LegacyOffer = "legacy_offer";
    public const string LegacyOffered = "legacy_offered";
    public const string Snoozed = "snoozed";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Dismissed = "dismissed";
}
