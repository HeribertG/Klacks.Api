// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Common;

public interface ISelectedGroupContextResolver
{
    Task<List<Guid>?> ResolveVisibleGroupIdsAsync();
}
