// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Klacksy.Models;

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface INavigationTargetCacheService
{
    IReadOnlyList<NavigationTarget> All { get; }
    NavigationTarget? GetById(string targetId);
    IReadOnlyList<NavigationTarget> FindBySynonym(string token, string locale);
    IReadOnlyList<NavigationTarget> FindBySynonymAnyLocale(string token);
    void Invalidate();
}
