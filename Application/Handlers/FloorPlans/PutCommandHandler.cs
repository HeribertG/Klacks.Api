// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlans;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<FloorPlanResource>, FloorPlanResource?>
{
    private readonly IFloorPlanRepository _floorPlanRepository;
    private readonly FloorPlanMapper _floorPlanMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IFloorPlanRepository floorPlanRepository,
        FloorPlanMapper floorPlanMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _floorPlanRepository = floorPlanRepository;
        _floorPlanMapper = floorPlanMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanResource?> Handle(PutCommand<FloorPlanResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _floorPlanRepository.Get(request.Resource.Id);

            if (existing == null)
            {
                throw new KeyNotFoundException($"FloorPlan with ID {request.Resource.Id} not found");
            }

            existing.Name = request.Resource.Name;
            existing.Description = request.Resource.Description;
            existing.CanvasJson = request.Resource.CanvasJson;
            existing.ThumbnailData = request.Resource.ThumbnailData;
            existing.UpdateTime = DateTime.UtcNow;

            await _floorPlanRepository.Put(existing);
            await _unitOfWork.CompleteAsync();

            return _floorPlanMapper.ToFloorPlanResource(existing);
        }, "UpdateFloorPlan", new { request.Resource.Id });
    }
}
