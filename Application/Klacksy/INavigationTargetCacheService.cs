// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Klacksy.Models;

public interface INavigationTargetCacheService
{
    IReadOnlyList<NavigationTarget> All { get; }
    NavigationTarget? GetById(string targetId);
    IReadOnlyList<NavigationTarget> FindBySynonym(string token, string locale);
    void Invalidate();
}
