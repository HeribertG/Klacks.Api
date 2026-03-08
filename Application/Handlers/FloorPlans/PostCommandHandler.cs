// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlans;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<FloorPlanResource>, FloorPlanResource?>
{
    private readonly IFloorPlanRepository _floorPlanRepository;
    private readonly FloorPlanMapper _floorPlanMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IFloorPlanRepository floorPlanRepository,
        FloorPlanMapper floorPlanMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _floorPlanRepository = floorPlanRepository;
        _floorPlanMapper = floorPlanMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanResource?> Handle(PostCommand<FloorPlanResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = _floorPlanMapper.ToFloorPlanEntity(request.Resource);
            entity.Id = Guid.NewGuid();
            entity.CreateTime = DateTime.UtcNow;

            await _floorPlanRepository.Add(entity);
            await _unitOfWork.CompleteAsync();

            return _floorPlanMapper.ToFloorPlanResource(entity);
        }, "CreateFloorPlan", new { request.Resource.Name });
    }
}
