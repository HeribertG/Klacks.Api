// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists every client's hourly availability in a date range. Entries of clients outside the caller's
/// group visibility are left out, as if they did not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the entries down to clients the calling user may see</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListClientAvailabilitiesQuery, IEnumerable<ClientAvailabilityResource>>
{
    private readonly IClientAvailabilityRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ClientAvailabilityMapper _mapper;

    public ListQueryHandler(
        IClientAvailabilityRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        ClientAvailabilityMapper mapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClientAvailabilityResource>> Handle(
        ListClientAvailabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = await _repository.GetByDateRange(request.StartDate, request.EndDate);
            var visible = await _clientVisibilityGuard.FilterVisibleAsync(
                entities, e => e.ClientId, cancellationToken);
            return visible.Select(_mapper.ToResource).ToList();
        }, "ListClientAvailabilities", new { request.StartDate, request.EndDate });
    }
}
