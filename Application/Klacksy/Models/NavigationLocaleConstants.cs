// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Locale code shared by navigation target caching and matching, so the English fallback locale
/// is declared once instead of repeated as a literal at every call site.
/// </summary>
public static class NavigationLocaleConstants
{
    public const string English = "en";
}
