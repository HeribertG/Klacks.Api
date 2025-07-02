namespace Klacks.Api.Interfaces
{
    public interface IGetAllClientIdsFromGroupAndSubgroups
    {
        Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId);

        Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds);
    }
}
