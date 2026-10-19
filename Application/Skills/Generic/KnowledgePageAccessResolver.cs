// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether the page a knowledge skill explains is open to the calling user. The client takes the user to
/// that page when the skill runs, but only if the route guard lets them in, so the answer must know which case applies.
/// The page right comes from the same page-key catalog navigate_to checks, never from a second copy.
/// </summary>
/// <param name="pageKeyCatalog">Scanner-generated page-key catalog holding each page's required right</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills.Generic;

public class KnowledgePageAccessResolver
{
    private readonly IKlacksyPageKeyCatalog _pageKeyCatalog;

    public KnowledgePageAccessResolver(IKlacksyPageKeyCatalog pageKeyCatalog)
    {
        _pageKeyCatalog = pageKeyCatalog;
    }

    public KnowledgePageAccess Resolve(string? pageKey, IReadOnlyList<string> userPermissions)
    {
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            return KnowledgePageAccess.NoPage;
        }

        var entry = _pageKeyCatalog.GetByPageKey(pageKey);
        if (entry == null)
        {
            return KnowledgePageAccess.NoPage;
        }

        return Permissions.HasAllRequiredPermissions(userPermissions, entry.RequiredPermission)
            ? KnowledgePageAccess.Open
            : KnowledgePageAccess.Denied;
    }
}
