// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Locale code shared by navigation target caching and matching, so the English fallback locale
/// is declared once instead of repeated as a literal at every call site.
/// </summary>
public static class NavigationLocaleConstants
{
    public const string English = "en";
}
