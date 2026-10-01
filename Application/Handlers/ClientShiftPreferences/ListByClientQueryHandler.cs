// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles listing all shift preferences for a specific client. A client outside the caller's group
/// visibility is answered exactly like a client that does not exist: with an empty list.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ClientShiftPreferences;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public class ListByClientQueryHandler : BaseHandler, IRequestHandler<ListByClientQuery, List<ClientShiftPreferenceResource>>
{
    private readonly IClientShiftPreferenceRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ClientShiftPreferenceMapper _mapper;

    public ListByClientQueryHandler(
        IClientShiftPreferenceRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        ClientShiftPreferenceMapper mapper,
        ILogger<ListByClientQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _mapper = mapper;
    }

    public async Task<List<ClientShiftPreferenceResource>> Handle(ListByClientQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.ClientId, cancellationToken))
            {
                return [];
            }

            var entities = await _repository.GetByClientIdAsync(request.ClientId, cancellationToken);
            return _mapper.ToResources(entities);
        },
        "listing shift preferences by client",
        new { request.ClientId });
    }
}
