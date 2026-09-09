// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Known values of NavigationTarget.Category as produced by the frontend target scanner
/// (Klacks.Ui/tools/scan-klacksy-targets.ts, PAGE_LEVEL_CATEGORY). A page-level entry has
/// TargetId == pageKey and no data-klacksy-target marker to scroll to - it stands for the page
/// itself, not an in-page spot.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public static class NavigationTargetCategories
{
    public const string PageLevel = "page";
}
