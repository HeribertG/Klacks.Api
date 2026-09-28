// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IGroupCacheService
{
    void InvalidateGroupHierarchyCache();
    void InvalidateGroupCache(Guid groupId);
}
