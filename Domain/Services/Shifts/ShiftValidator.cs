using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;

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

    public async Task EnsureNoOriginalShiftCopyExists(Guid originalShiftId, IShiftRepository shiftRepository)
    {
        var existingCopy = await shiftRepository
            .GetQuery()
            .Where(s => s.OriginalId == originalShiftId && s.Status == ShiftStatus.OriginalShift)
            .FirstOrDefaultAsync();

        if (existingCopy != null)
        {
            throw new InvalidOperationException(
                $"An OriginalShift copy already exists for OriginalId={originalShiftId}. Cannot create duplicate copy.");
        }
    }
}
