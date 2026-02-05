namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IGroupCacheService
{
    void InvalidateGroupHierarchyCache();
    void InvalidateGroupCache(Guid groupId);
}
