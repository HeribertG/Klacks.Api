// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Models.Email;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class EmailFolderMapper
{
    [MapperIgnoreTarget(nameof(EmailFolderResource.UnreadCount))]
    [MapperIgnoreTarget(nameof(EmailFolderResource.TotalCount))]
    public partial EmailFolderResource ToResource(EmailFolder folder);
}
