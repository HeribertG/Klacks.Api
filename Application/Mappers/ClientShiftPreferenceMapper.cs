// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mapperly mapper for ClientShiftPreference entity and resource conversions.
/// </summary>
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Models.Associations;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class ClientShiftPreferenceMapper
{
    public ClientShiftPreferenceResource ToResource(ClientShiftPreference entity)
    {
        return new ClientShiftPreferenceResource
        {
            Id = entity.Id,
            ClientId = entity.ClientId,
            ShiftId = entity.ShiftId,
            PreferenceType = entity.PreferenceType,
            ShiftName = entity.Shift?.Name,
            ShiftAbbreviation = entity.Shift?.Abbreviation,
        };
    }

    public List<ClientShiftPreferenceResource> ToResources(List<ClientShiftPreference> entities)
    {
        return entities.Select(ToResource).ToList();
    }

    [MapperIgnoreTarget(nameof(ClientShiftPreference.Client))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.Shift))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.CreateTime))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.UpdateTime))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.DeletedTime))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.IsDeleted))]
    [MapperIgnoreTarget(nameof(ClientShiftPreference.CurrentUserDeleted))]
    public partial ClientShiftPreference ToEntity(ClientShiftPreferenceResource resource);
}
