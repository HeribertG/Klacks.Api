namespace Klacks.Api.Domain.Services.ShiftSchedule;

public interface IShiftGroupFilterService
{
    Task<List<Guid>> GetVisibleGroupIdsAsync(Guid? selectedGroupId);
}
