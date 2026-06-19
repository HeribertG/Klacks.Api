// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving all navigation targets, optionally filtered by synonym status.
/// </summary>

namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Infrastructure.Mediator;

public sealed class GetNavigationTargetsQueryHandler : IRequestHandler<GetNavigationTargetsQuery, IReadOnlyList<NavigationTarget>>
{
    private readonly INavigationTargetCacheService _cache;

    public GetNavigationTargetsQueryHandler(INavigationTargetCacheService cache) => _cache = cache;

    public Task<IReadOnlyList<NavigationTarget>> Handle(GetNavigationTargetsQuery query, CancellationToken cancellationToken)
    {
        var list = _cache.All.AsEnumerable();
        if (!string.IsNullOrEmpty(query.Status))
            list = list.Where(t => t.SynonymStatus == query.Status);
        return Task.FromResult<IReadOnlyList<NavigationTarget>>(list.ToList());
    }
}
