using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftGroupFilterService : IShiftGroupFilterService
{
    private readonly IGroupVisibilityService _groupVisibility;

    public ShiftGroupFilterService(IGroupVisibilityService groupVisibility)
    {
        _groupVisibility = groupVisibility;
    }

    public async Task<List<Guid>> GetVisibleGroupIdsAsync(Guid? selectedGroupId)
    {
        if (selectedGroupId.HasValue)
        {
            return [selectedGroupId.Value];
        }

        if (await _groupVisibility.IsAdmin())
        {
            return [];
        }

        return await _groupVisibility.ReadVisibleRootIdList();
    }
}
