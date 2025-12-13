namespace Klacks.Api.Domain.Interfaces;

public interface IGroupCacheService
{
    void InvalidateGroupHierarchyCache();
    void InvalidateGroupCache(Guid groupId);
}
