// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Domain.Models.Imports;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class ErpDropPointMapper
{
    public partial ErpDropPointResource ToResource(ErpDropPoint entity);
    public partial List<ErpDropPointResource> ToResources(List<ErpDropPoint> entities);

    public partial ErpDropPointListResource ToListResource(ErpDropPoint entity);
    public partial List<ErpDropPointListResource> ToListResources(List<ErpDropPoint> entities);

    [MapperIgnoreTarget(nameof(ErpDropPoint.CreateTime))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.UpdateTime))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.DeletedTime))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.IsDeleted))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.LastPolledAt))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.LastError))]
    public partial ErpDropPoint ToEntity(ErpDropPointResource resource);

    [MapperIgnoreTarget(nameof(ErpDropPoint.Id))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.CreateTime))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.UpdateTime))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.DeletedTime))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.IsDeleted))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.LastPolledAt))]
    [MapperIgnoreTarget(nameof(ErpDropPoint.LastError))]
    public partial void UpdateEntity(ErpDropPointResource resource, ErpDropPoint target);
}
