namespace Klacks.Api.Infrastructure.Interfaces
{
    public interface IGetAllClientIdsFromGroupAndSubgroups
    {
        Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId);

        Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds);
    }
}
