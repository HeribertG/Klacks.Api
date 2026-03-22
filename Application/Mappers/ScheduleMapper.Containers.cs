// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partial class for ContainerTemplate, Route, RouteLocation and RouteSegment mappings.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

public partial class ScheduleMapper
{
    [MapperIgnoreSource(nameof(ContainerTemplate.Shift))]
    [MapperIgnoreSource(nameof(ContainerTemplate.ContainerTemplateItems))]
    private partial ContainerTemplateResource ToContainerTemplateResourceBase(ContainerTemplate template);

    public ContainerTemplateResource ToContainerTemplateResource(ContainerTemplate template)
    {
        var resource = ToContainerTemplateResourceBase(template);
        if (template.Shift != null)
        {
            resource.Shift = ToShiftResource(template.Shift);
        }
        resource.ContainerTemplateItems = template.ContainerTemplateItems
            .Select(ToContainerTemplateItemResource)
            .ToList();
        return resource;
    }

    [MapperIgnoreTarget(nameof(ContainerTemplate.CreateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.UpdateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.DeletedTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.IsDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.Shift))]
    public partial ContainerTemplate ToContainerTemplateEntity(ContainerTemplateResource resource);

    public partial RouteInfoResource ToRouteInfoResource(RouteInfo routeInfo);
    public partial RouteInfo ToRouteInfoEntity(RouteInfoResource resource);

    public partial RouteLocationResource ToRouteLocationResource(RouteLocation location);
    public partial RouteLocation ToRouteLocationEntity(RouteLocationResource resource);

    public partial RouteSegmentDirectionsResource ToRouteSegmentDirectionsResource(RouteSegmentDirections directions);
    public partial RouteSegmentDirections ToRouteSegmentDirectionsEntity(RouteSegmentDirectionsResource resource);

    public partial DirectionStepResource ToDirectionStepResource(DirectionStep step);
    public partial DirectionStep ToDirectionStepEntity(DirectionStepResource resource);

    [MapperIgnoreTarget(nameof(ContainerTemplateItemResource.Weekday))]
    [MapperIgnoreSource(nameof(ContainerTemplateItem.Shift))]
    [MapperIgnoreSource(nameof(ContainerTemplateItem.Absence))]
    private partial ContainerTemplateItemResource ToContainerTemplateItemResourceBase(ContainerTemplateItem item);

    public ContainerTemplateItemResource ToContainerTemplateItemResource(ContainerTemplateItem item)
    {
        var resource = ToContainerTemplateItemResourceBase(item);
        if (item.Shift != null)
        {
            resource.Shift = ToShiftResource(item.Shift);
        }
        if (item.Absence != null)
        {
            resource.Absence = ToAbsenceResource(item.Absence);
        }
        return resource;
    }

    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CreateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.UpdateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.DeletedTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.IsDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.Shift))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.Absence))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.ContainerTemplate))]
    public partial ContainerTemplateItem ToContainerTemplateItemEntity(ContainerTemplateItemResource resource);

    public partial AbsenceResource ToAbsenceResource(Absence absence);
}
