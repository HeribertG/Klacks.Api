// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving all children (sub-works, sub-breaks, and work changes) of a container work.
/// </summary>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class GetContainerWorkChildrenQueryHandler : BaseHandler, IRequestHandler<GetContainerWorkChildrenQuery, ContainerWorkChildrenResource>
{
    private readonly IContainerWorkChildrenReadRepository _childrenReadRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetContainerWorkChildrenQueryHandler(
        IContainerWorkChildrenReadRepository childrenReadRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetContainerWorkChildrenQueryHandler> logger)
        : base(logger)
    {
        _childrenReadRepository = childrenReadRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ContainerWorkChildrenResource> Handle(GetContainerWorkChildrenQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var parentWork = await _childrenReadRepository.GetParentWorkNoTracking(request.WorkId, cancellationToken);

            var subWorks = await _childrenReadRepository.GetChildWorksWithShiftClient(request.WorkId, cancellationToken);

            var subBreaks = await _childrenReadRepository.GetChildBreaksWithAbsence(request.WorkId, cancellationToken);

            var subWorkIds = subWorks.Select(w => w.Id).ToList();

            var subWorkChanges = await _childrenReadRepository.GetWorkChangesForWorks(subWorkIds, cancellationToken);

            var startBase = parentWork?.StartBase;
            var endBase = parentWork?.EndBase;
            int? transportMode = parentWork?.TransportMode.HasValue == true ? (int)parentWork.TransportMode.Value : null;

            var needsFallback = parentWork != null
                && parentWork.ShiftId != Guid.Empty
                && (string.IsNullOrEmpty(startBase) || string.IsNullOrEmpty(endBase) || transportMode == null);

            if (needsFallback)
            {
                var weekday = (int)parentWork!.CurrentDate.DayOfWeek;
                var template = await _childrenReadRepository.GetContainerTemplate(
                    parentWork.ShiftId, weekday, request.IsHoliday, cancellationToken);

                if (template != null)
                {
                    if (string.IsNullOrEmpty(startBase)) startBase = template.StartBase;
                    if (string.IsNullOrEmpty(endBase)) endBase = template.EndBase;
                    if (transportMode == null) transportMode = (int)template.TransportMode;
                }
            }

            return new ContainerWorkChildrenResource
            {
                SubWorks = subWorks.Select(_scheduleMapper.ToWorkResource).ToList(),
                SubBreaks = subBreaks.Select(_scheduleMapper.ToBreakResource).ToList(),
                SubWorkChanges = subWorkChanges.Select(_scheduleMapper.ToWorkChangeResource).ToList(),
                ParentStartBase = startBase,
                ParentEndBase = endBase,
                ParentTransportMode = transportMode
            };
        }, nameof(Handle), new { request.WorkId, request.IsHoliday });
    }
}
