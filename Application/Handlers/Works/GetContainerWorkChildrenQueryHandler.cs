// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving all children (sub-works, sub-breaks, and work changes) of a container work.
/// A container owned by a client outside the caller's group visibility is answered exactly like a container
/// that does not exist: with an empty result.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the container owner</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class GetContainerWorkChildrenQueryHandler : BaseHandler, IRequestHandler<GetContainerWorkChildrenQuery, ContainerWorkChildrenResource>
{
    private readonly IContainerWorkChildrenReadRepository _childrenReadRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetContainerWorkChildrenQueryHandler(
        IContainerWorkChildrenReadRepository childrenReadRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetContainerWorkChildrenQueryHandler> logger)
        : base(logger)
    {
        _childrenReadRepository = childrenReadRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ContainerWorkChildrenResource> Handle(GetContainerWorkChildrenQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var parentWork = await _childrenReadRepository.GetParentWorkNoTracking(request.WorkId, cancellationToken);
            if (parentWork != null && !await _clientVisibilityGuard.IsVisibleAsync(parentWork.ClientId, cancellationToken))
            {
                return new ContainerWorkChildrenResource();
            }

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
