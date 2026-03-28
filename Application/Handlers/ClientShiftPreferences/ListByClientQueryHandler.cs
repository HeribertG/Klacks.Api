// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles listing all shift preferences for a specific client.
/// </summary>
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ClientShiftPreferences;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public class ListByClientQueryHandler : BaseHandler, IRequestHandler<ListByClientQuery, List<ClientShiftPreferenceResource>>
{
    private readonly IClientShiftPreferenceRepository _repository;
    private readonly ClientShiftPreferenceMapper _mapper;

    public ListByClientQueryHandler(
        IClientShiftPreferenceRepository repository,
        ClientShiftPreferenceMapper mapper,
        ILogger<ListByClientQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<ClientShiftPreferenceResource>> Handle(ListByClientQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = await _repository.GetByClientIdAsync(request.ClientId, cancellationToken);
            return _mapper.ToResources(entities);
        },
        "listing shift preferences by client",
        new { request.ClientId });
    }
}
