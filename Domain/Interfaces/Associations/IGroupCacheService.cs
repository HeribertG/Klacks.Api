// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IGroupCacheService
{
    void InvalidateGroupHierarchyCache();
    void InvalidateGroupCache(Guid groupId);
}
