// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.ErpDropPoints;
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, ErpDropPointResource?>
{
    private readonly IErpDropPointRepository _repository;
    private readonly ErpDropPointMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IErpDropPointRepository repository,
        ErpDropPointMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ErpDropPointResource?> Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting ERP drop point: {Id}", request.Id);

        var entity = await _repository.Delete(request.Id);
        if (entity == null)
        {
            _logger.LogWarning("ERP drop point not found: {Id}", request.Id);
            return null;
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Deleted ERP drop point: {Id}", request.Id);

        return _mapper.ToResource(entity);
    }
}
