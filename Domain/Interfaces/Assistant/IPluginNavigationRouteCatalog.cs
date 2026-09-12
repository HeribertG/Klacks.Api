// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Domain-side read access to the navigation routes feature plugins register for themselves.
/// A plugin route is not part of the scanner-generated page-key manifest (the scanner only sees
/// Klacks.Ui source), so navigate_to would otherwise reject an installed plugin's page as unknown.
/// Implemented in the Application layer on top of the skill registry, mirroring the
/// IKlacksyPageKeyCatalog / INavigationTargetCatalog dependency-inversion pattern.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPluginNavigationRouteCatalog
{
    /// <summary>
    /// Returns the Angular route a feature plugin registered under this page key, or null when no
    /// installed plugin claims it.
    /// </summary>
    /// <param name="pageKey">Page key as passed to navigate_to; matched case-insensitively.</param>
    string? GetRoute(string pageKey);
}
