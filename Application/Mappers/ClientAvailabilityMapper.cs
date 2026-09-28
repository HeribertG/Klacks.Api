// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Models.Staffs;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class ClientAvailabilityMapper
{
    public partial ClientAvailabilityResource ToResource(ClientAvailability entity);

    [MapperIgnoreTarget(nameof(ClientAvailability.CreateTime))]
    [MapperIgnoreTarget(nameof(ClientAvailability.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ClientAvailability.UpdateTime))]
    [MapperIgnoreTarget(nameof(ClientAvailability.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ClientAvailability.DeletedTime))]
    [MapperIgnoreTarget(nameof(ClientAvailability.IsDeleted))]
    [MapperIgnoreTarget(nameof(ClientAvailability.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ClientAvailability.Client))]
    public partial ClientAvailability ToEntity(ClientAvailabilityResource resource);
}
