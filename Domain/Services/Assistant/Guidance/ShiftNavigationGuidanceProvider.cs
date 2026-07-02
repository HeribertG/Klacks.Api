// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Supplies the shift lock-guidance note when navigate_to opens the edit-shift page directly
/// with a GUID (e.g. after an ambiguity dialog), so the model learns about the sealed-order
/// lock state on every navigation path, not only via search_and_navigate single matches.
/// </summary>
/// <param name="shiftRepository">Repository used to read the shift's lifecycle status by id</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Domain.Services.Assistant.Guidance;

public class ShiftNavigationGuidanceProvider : INavigationGuidanceProvider
{
    private readonly IShiftRepository _shiftRepository;

    public ShiftNavigationGuidanceProvider(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public bool CanHandle(string pageKey) =>
        string.Equals(pageKey, UiPageKeys.EditShift, StringComparison.OrdinalIgnoreCase);

    public async Task<string?> GetGuidanceAsync(string pageKey, Guid entityId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetNoTracking(entityId);
        return shift is null ? null : ShiftLockGuidanceText.ForStatus(shift.Status);
    }
}
