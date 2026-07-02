// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class GetQueryHandler : IRequestHandler<GetQuery, ErpDropPointResource?>
{
    private readonly IErpDropPointRepository _repository;
    private readonly ErpDropPointMapper _mapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(
        IErpDropPointRepository repository,
        ErpDropPointMapper mapper,
        ILogger<GetQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ErpDropPointResource?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching ERP drop point with ID: {Id}", request.Id);

        var entity = await _repository.Get(request.Id);
        if (entity == null)
        {
            _logger.LogWarning("ERP drop point not found: {Id}", request.Id);
            return null;
        }

        return _mapper.ToResource(entity);
    }
}
