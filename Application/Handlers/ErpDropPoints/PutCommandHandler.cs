// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.ErpDropPoints;
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class PutCommandHandler : IRequestHandler<PutCommand, ErpDropPointResource?>
{
    private readonly IErpDropPointRepository _repository;
    private readonly ErpDropPointMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IErpDropPointRepository repository,
        ErpDropPointMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ErpDropPointResource?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating ERP drop point: {Id}", request.Model.Id);

        var existingEntity = await _repository.Get(request.Model.Id);
        if (existingEntity == null)
        {
            _logger.LogWarning("ERP drop point not found: {Id}", request.Model.Id);
            return null;
        }

        _mapper.UpdateEntity(request.Model, existingEntity);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Updated ERP drop point: {Id}", request.Model.Id);

        return _mapper.ToResource(existingEntity);
    }
}
