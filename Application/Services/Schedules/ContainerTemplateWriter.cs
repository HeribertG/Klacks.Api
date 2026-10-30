// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Writes the weekday templates of a container the way the Ui container page saves an autofill result and confirms them by
/// re-reading: the planned visits become the items, the route info is mapped to the template resource, the templates are
/// posted under the container lock the caller holds, and every written template must come back with its visit times, travel
/// times and route info, otherwise a SkillVerificationException aborts the surrounding transaction. Also loads the task
/// shifts an autofill picked.
/// </summary>
/// <param name="mediator">Posts the templates and reads them back</param>
/// <param name="scheduleMapper">Maps the built RouteInfo to the template resource</param>
/// <param name="shiftRepository">Loads the picked task shifts</param>

using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ContainerTemplateWriter
{
    private readonly IMediator _mediator;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IShiftRepository _shiftRepository;

    public ContainerTemplateWriter(IMediator mediator, ScheduleMapper scheduleMapper, IShiftRepository shiftRepository)
    {
        _mediator = mediator;
        _scheduleMapper = scheduleMapper;
        _shiftRepository = shiftRepository;
    }

    /// <param name="skillName">Skill that asked, carried on a refusal</param>
    /// <param name="selectedShiftIds">Ids of the picked task shifts</param>
    public async Task<List<Shift>> LoadTaskShiftsAsync(string skillName, IEnumerable<Guid> selectedShiftIds)
    {
        var taskShifts = new List<Shift>();
        foreach (var shiftId in selectedShiftIds)
        {
            var taskShift = await _shiftRepository.Get(shiftId);
            if (taskShift is null)
            {
                throw new SkillVerificationException(
                    skillName,
                    $"Selected task shift {shiftId} could not be re-read while building the container template.");
            }

            taskShifts.Add(taskShift);
        }

        return taskShifts;
    }

    /// <param name="containerId">Container the template belongs to</param>
    /// <param name="storageWeekday">Stored weekday 0=Sunday..6=Saturday</param>
    /// <param name="fromTime">Start of the day's time budget</param>
    /// <param name="untilTime">End of the day's time budget</param>
    /// <param name="location">Base address the route starts and ends at</param>
    /// <param name="transportMode">Transport mode of the route</param>
    /// <param name="planned">The placed visits with their route info</param>
    public ContainerTemplateResource BuildTemplate(
        Guid containerId,
        int storageWeekday,
        TimeOnly fromTime,
        TimeOnly untilTime,
        string location,
        ContainerTransportMode transportMode,
        PlannedContainerVisits planned) => new()
        {
            Id = Guid.Empty,
            ContainerId = containerId,
            FromTime = fromTime,
            UntilTime = untilTime,
            Weekday = storageWeekday,
            IsHoliday = false,
            IsWeekdayAndHoliday = false,
            StartBase = location,
            EndBase = location,
            TransportMode = transportMode,
            ContainerTemplateItems = planned.Items,
            RouteInfo = _scheduleMapper.ToRouteInfoResource(planned.RouteInfo)
        };

    /// <param name="skillName">Skill that asked, carried on a refusal</param>
    /// <param name="containerId">Container the templates belong to; its template lock is held by the caller</param>
    /// <param name="templates">The templates to post, at most one per weekday</param>
    public async Task WriteAndConfirmAsync(
        string skillName, Guid containerId, List<ContainerTemplateResource> templates, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PostContainerTemplatesCommand(containerId, templates), cancellationToken);

        foreach (var written in templates)
        {
            var isoWeekday = IsoWeekday.FromStorage(written.Weekday);
            await SkillWriteSupport.ConfirmPersistedAsync(
                skillName,
                () => _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken),
                stored => IsTemplateFullyPersisted(stored, written.Weekday, written),
                $"the container template for weekday {isoWeekday} with its visit times, travel times and route info");
        }
    }

    private static bool IsTemplateFullyPersisted(
        List<ContainerTemplateResource> templates, int storageWeekday, ContainerTemplateResource written)
    {
        var template = templates.FirstOrDefault(t => t.Weekday == storageWeekday && !t.IsHoliday)
            ?? templates.FirstOrDefault(t => t.Weekday == storageWeekday);

        return template != null && ContainerVisitPlacement.StoredVisitTimesMatch(written, template);
    }
}
