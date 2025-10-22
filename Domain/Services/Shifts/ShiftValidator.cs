using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftValidator : IShiftValidator
{
    public void EnsureUniqueGroupItems(ICollection<GroupItem> groupItems)
    {
        var duplicateGroups = groupItems
            .GroupBy(gi => gi.GroupId)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var duplicateGroup in duplicateGroups)
        {
            var itemsToRemove = duplicateGroup.Take(duplicateGroup.Count() - 1).ToList();

            foreach (var item in itemsToRemove)
            {
                groupItems.Remove(item);
            }
        }
    }
}
