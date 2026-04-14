// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query and handler for retrieving all navigation targets, optionally filtered by synonym status and locale.
/// </summary>
/// <param name="Status">Optional synonym status filter (e.g. "pending", "generated", "approved")</param>
/// <param name="Locale">Optional locale filter (reserved for future use)</param>
namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Application.Klacksy;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Infrastructure.Mediator;

public record GetNavigationTargetsQuery(string? Status, string? Locale) : IRequest<IReadOnlyList<NavigationTarget>>;

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
