// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.ClientAvailabilities;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class BulkUpdateCommandHandler : BaseHandler, IRequestHandler<BulkUpdateClientAvailabilityCommand, int>
{
    private readonly IClientAvailabilityRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ClientAvailabilityMapper _mapper;

    public BulkUpdateCommandHandler(
        IClientAvailabilityRepository repository,
        IUnitOfWork unitOfWork,
        ClientAvailabilityMapper mapper,
        ILogger<BulkUpdateCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<int> Handle(
        BulkUpdateClientAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = command.Request.Items
                .Select(_mapper.ToEntity)
                .ToList();

            await _repository.BulkUpsert(entities);
            await _unitOfWork.CompleteAsync();

            return entities.Count;
        }, "BulkUpdateClientAvailability", new { Count = command.Request.Items.Count });
    }
}
