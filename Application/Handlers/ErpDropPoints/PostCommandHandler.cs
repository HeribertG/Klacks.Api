// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.ErpDropPoints;
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class PostCommandHandler : IRequestHandler<PostCommand, ErpDropPointResource?>
{
    private readonly IErpDropPointRepository _repository;
    private readonly ErpDropPointMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IErpDropPointRepository repository,
        ErpDropPointMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ErpDropPointResource?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new ERP drop point: {Name}", request.Model.Name.ForLog());

        var entity = _mapper.ToEntity(request.Model);
        entity.Id = Guid.NewGuid();

        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Created ERP drop point with ID: {Id}", entity.Id);

        return _mapper.ToResource(entity);
    }
}
