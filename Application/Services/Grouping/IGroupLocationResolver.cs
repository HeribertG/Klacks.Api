// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public interface IGroupLocationResolver
{
    Task<GroupLocationResolveResult> ResolveAsync(Guid groupId, CancellationToken cancellationToken = default);
}
