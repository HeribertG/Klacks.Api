// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Defaults for the company-rule intake dialog: the time-of-day format accepted for surcharge-window
/// parameters and the lifetime after which an abandoned draft is pruned from the pending store.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class CompanyRuleDraftDefaults
{
    public const int PendingDraftTtlMinutes = 60;

    public const string TimeOfDayFormat = "HH:mm";
}
