// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="GetReplacementCandidatesQuery"/>: dispatches the unchanged find_replacement search and
/// adds a phone number to each eligible candidate. The phone resolver applies group visibility itself, so a
/// candidate the caller may not see never gets a number even if the search returned it.
/// </summary>
/// <param name="mediator">Dispatches the FindReplacementQuery</param>
/// <param name="phoneResolver">Visible candidates' phone numbers, read with one query</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class GetReplacementCandidatesQueryHandler
    : IRequestHandler<GetReplacementCandidatesQuery, ReplacementSearchResult>
{
    private readonly IMediator _mediator;
    private readonly IReplacementContactPhoneResolver _phoneResolver;

    public GetReplacementCandidatesQueryHandler(IMediator mediator, IReplacementContactPhoneResolver phoneResolver)
    {
        _mediator = mediator;
        _phoneResolver = phoneResolver;
    }

    public async Task<ReplacementSearchResult> Handle(
        GetReplacementCandidatesQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request.Search, cancellationToken);
        if (result.Eligible.Count == 0)
        {
            return result;
        }

        var phones = await _phoneResolver.ResolveAsync(
            result.Eligible.Select(c => c.ClientId).ToList(), cancellationToken);

        var eligible = result.Eligible
            .Select(c => phones.TryGetValue(c.ClientId, out var phone) ? c with { Phone = phone } : c)
            .ToList();

        return result with { Eligible = eligible };
    }
}
