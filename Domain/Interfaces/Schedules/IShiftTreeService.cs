using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftTreeService
{
    void SetHierarchyRelation(Shift shift, Guid? parentId, Guid? parentRootId);
    Task RecalculateNestedSetValuesAsync(Guid rootId);
    Task RecalculateAllAffectedTreesAsync(List<Shift> processedShifts);
    int CalculateTreeWidth(int leftValue, int rightValue);
}
