namespace Klacks.Api.Domain.Interfaces;

public interface IGroupTreeDatabaseAdapter
{
    Task UpdateRgtValuesAsync(int minRgt, Guid root, int adjustment);
    Task UpdateLftValuesAsync(int minLft, Guid root, int adjustment);
    Task UpdateRgtRangeAsync(int minRgt, int maxRgt, Guid root, int adjustment);
    Task UpdateLftRangeAsync(int minLft, int maxLft, Guid root, int adjustment);

    Task MarkSubtreeAsDeletedAsync(int lft, int rgt, Guid root, DateTime deletedTime);
    Task ShiftNodesAfterDeleteAsync(int afterRgt, Guid root, int width);

    Task MarkSubtreeWithNegativeValuesAsync(int lft, int rgt, Guid root);
    Task ShiftNodesToCloseGapAsync(int afterRgt, Guid root, int width);
    Task MakeSpaceAtPositionAsync(int position, Guid root, int width);
    Task MoveMarkedSubtreeAsync(int offset, Guid newRoot, Guid oldRoot);
}
