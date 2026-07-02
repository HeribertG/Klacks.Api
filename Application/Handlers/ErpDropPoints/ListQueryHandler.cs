// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ErpDropPointListResource>>
{
    private readonly IErpDropPointRepository _repository;
    private readonly ErpDropPointMapper _mapper;
    private readonly ILogger<ListQueryHandler> _logger;

    public ListQueryHandler(
        IErpDropPointRepository repository,
        ErpDropPointMapper mapper,
        ILogger<ListQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ErpDropPointListResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching ERP drop points list");

        var entities = await _repository.List();
        var sortedEntities = entities.OrderBy(e => e.Name).ToList();

        _logger.LogInformation("Found {Count} ERP drop points", sortedEntities.Count);

        return _mapper.ToListResources(sortedEntities);
    }
}
