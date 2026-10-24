// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Defaults of the guided open-orders review (start_order_review .. execute_order_review): the start proposed for a
/// full-day order, the standard split into equal parts, the smallest bundle that is reviewed as a pattern, the number of
/// orders one confirmed execution changes, the lifetime of an abandoned draft and the length of the review keys.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class OrderReviewDefaults
{
    public const int ProposedFullDayStartHour = 7;

    public const int StandardPartCount = 3;

    public const int AlternativePartCount = 2;

    public const int HoursPerDay = 24;

    public const int MinBundleSize = 3;

    public const int MaxOrdersPerExecution = 25;

    public const int PendingDraftTtlMinutes = 240;

    public const string BundleKeyPrefix = "B";

    public const string SingleKeyPrefix = "S";

    public const string DraftScopeFallbackKey = "order-review";

    public static readonly TimeOnly ProposedFullDayStart = new(ProposedFullDayStartHour, 0);
}
