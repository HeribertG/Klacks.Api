// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Decides whether a navigation target can only be opened for a specific record. The fast path sends a
/// bare route without an entity id; on an entity editor (edit-address, edit-group, edit-shift, ...) the
/// frontend then opens a blank NEW record — and a user already editing a record leaves it. Such targets
/// must go through the LLM, which knows the selected record from the page context.
/// A target that is itself a page key uses that page's own flag, so a creation page sharing the editor
/// route (new-employee on /workplace/edit-address) stays allowed. Any other target counts as requiring a
/// record as soon as its route belongs to a page that expects an entity id.
/// </summary>
/// <param name="pageKeyCatalog">Catalog of page keys with their routes and entity-id requirement</param>
public sealed class NavigationEntityRouteGuard : INavigationEntityRouteGuard
{
    private readonly IKlacksyPageKeyCatalog _pageKeyCatalog;

    public NavigationEntityRouteGuard(IKlacksyPageKeyCatalog pageKeyCatalog)
    {
        _pageKeyCatalog = pageKeyCatalog;
    }

    public bool RequiresEntity(string? targetId, string? route)
    {
        if (!string.IsNullOrEmpty(targetId))
        {
            var ownPage = _pageKeyCatalog.GetByPageKey(targetId);
            if (ownPage != null)
            {
                return ownPage.HasEntityParam;
            }
        }

        if (string.IsNullOrEmpty(route))
        {
            return false;
        }

        return _pageKeyCatalog.All.Any(entry =>
            entry.HasEntityParam && string.Equals(entry.Route, route, StringComparison.OrdinalIgnoreCase));
    }
}
