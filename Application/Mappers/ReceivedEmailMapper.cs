// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Models.Email;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class ReceivedEmailMapper
{
    public partial ReceivedEmailResource ToResource(ReceivedEmail email);

    public partial List<ReceivedEmailResource> ToResources(List<ReceivedEmail> emails);

    public partial ReceivedEmailListResource ToListResource(ReceivedEmail email);

    public partial List<ReceivedEmailListResource> ToListResources(List<ReceivedEmail> emails);
}
