// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that resolves the default ERP drop point, creating it with the shared default
/// values on first use, and maps it to its resource representation.
/// </summary>
/// <param name="request">Marker query without parameters</param>
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class GetDefaultQueryHandler : IRequestHandler<GetDefaultQuery, ErpDropPointResource>
{
    private readonly IErpDefaultDropPointProvider _defaultDropPointProvider;
    private readonly ErpDropPointMapper _mapper;

    public GetDefaultQueryHandler(IErpDefaultDropPointProvider defaultDropPointProvider, ErpDropPointMapper mapper)
    {
        _defaultDropPointProvider = defaultDropPointProvider;
        _mapper = mapper;
    }

    public async Task<ErpDropPointResource> Handle(GetDefaultQuery request, CancellationToken cancellationToken)
    {
        var dropPoint = await _defaultDropPointProvider.GetOrCreateDefaultAsync(cancellationToken);
        return _mapper.ToResource(dropPoint);
    }
}
