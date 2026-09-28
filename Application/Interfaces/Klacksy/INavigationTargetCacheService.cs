// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Klacksy.Models;

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface INavigationTargetCacheService
{
    IReadOnlyList<NavigationTarget> All { get; }
    NavigationTarget? GetById(string targetId);
    IReadOnlyList<NavigationTarget> GetByRoute(string route);
    IReadOnlyList<NavigationTarget> FindBySynonym(string token, string locale);
    IReadOnlyList<NavigationTarget> FindBySynonymAnyLocale(string token);
    void Invalidate();
    Task WarmUpAsync(CancellationToken cancellationToken = default);
}
