using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftValidator
{
    void EnsureUniqueGroupItems(ICollection<GroupItem> groupItems);
    Task EnsureNoOriginalShiftCopyExists(Guid originalShiftId, IShiftRepository shiftRepository);
}
