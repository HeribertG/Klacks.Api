// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlans;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<FloorPlanResource>, FloorPlanResource?>
{
    private readonly IFloorPlanRepository _floorPlanRepository;
    private readonly FloorPlanMapper _floorPlanMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IFloorPlanRepository floorPlanRepository,
        FloorPlanMapper floorPlanMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _floorPlanRepository = floorPlanRepository;
        _floorPlanMapper = floorPlanMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanResource?> Handle(DeleteCommand<FloorPlanResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = await _floorPlanRepository.Get(request.Id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"FloorPlan with ID {request.Id} not found");
            }

            await _floorPlanRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return _floorPlanMapper.ToFloorPlanResource(entity);
        }, "DeleteFloorPlan", new { request.Id });
    }
}
