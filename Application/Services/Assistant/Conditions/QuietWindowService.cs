// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default implementation of IQuietWindowService. Checks three independent quiet reasons, cheapest
/// first so a running job or import never pays for the entity lookup: (a) any AutofillStartGuard run
/// lock held - a strict superset of every per-family JobRegistry, because WizardBenchmarkService
/// acquires the guard's lock without registering into a JobRegistry at all; (b) an ERP import in
/// progress (ErpImportRunState, the flag F4 of the plan asked for since IErpOrderImportRunner exposed
/// no running state); (c) the condition's target Shift is sealed for its FromDate - a single day, not
/// the whole FromDate..UntilDate span, because a long-lived container/order condition must not go
/// permanently quiet over one sealed day elsewhere in its range (that would also starve the Etappe 5
/// AttemptCount escalation, which a quiet skip never increments). This deliberately diverges from
/// DayLockService's write guard, which lets a scenario-token write through a sealed day: 4f gates
/// whether Klacksy PROPOSES a remediation, not whether the write itself would be allowed, so a
/// scenario-capable remediation still stays quiet on a sealed target - the human sealed it on purpose.
/// </summary>
/// <param name="autofillStartGuard">Cross-family run-lock signal for (a).</param>
/// <param name="erpImportRunState">Running flag for (b).</param>
/// <param name="shiftRepository">Resolves the condition's EntityId to its Shift for (c).</param>
/// <param name="workRepository">Item-level LockLevel check on the shift's Work rows for (c).</param>
/// <param name="sealedDayRepository">Day-level seal check for (c).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public class QuietWindowService : IQuietWindowService
{
    private readonly AutofillStartGuard _autofillStartGuard;
    private readonly ErpImportRunState _erpImportRunState;
    private readonly IShiftRepository _shiftRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ISealedDayRepository _sealedDayRepository;

    public QuietWindowService(
        AutofillStartGuard autofillStartGuard,
        ErpImportRunState erpImportRunState,
        IShiftRepository shiftRepository,
        IWorkRepository workRepository,
        ISealedDayRepository sealedDayRepository)
    {
        _autofillStartGuard = autofillStartGuard;
        _erpImportRunState = erpImportRunState;
        _shiftRepository = shiftRepository;
        _workRepository = workRepository;
        _sealedDayRepository = sealedDayRepository;
    }

    public async Task<bool> IsQuietForAsync(AgentCondition condition, CancellationToken cancellationToken = default)
    {
        if (_autofillStartGuard.HasActiveRuns)
        {
            return true;
        }

        if (_erpImportRunState.IsRunning)
        {
            return true;
        }

        return await IsTargetEntitySealedAsync(condition, cancellationToken);
    }

    private async Task<bool> IsTargetEntitySealedAsync(AgentCondition condition, CancellationToken cancellationToken)
    {
        if (condition.EntityId is not { } entityId)
        {
            return false;
        }

        var shift = await _shiftRepository.GetNoTracking(entityId);
        if (shift == null)
        {
            return false;
        }

        if (await _workRepository.HasLockedWorkForShiftAsync(entityId, cancellationToken))
        {
            return true;
        }

        return await IsDaySealedAsync(shift.FromDate, condition.GroupId, cancellationToken);
    }

    private async Task<bool> IsDaySealedAsync(DateOnly date, Guid? groupId, CancellationToken cancellationToken)
    {
        var sealedRows = await _sealedDayRepository.GetRangeAsync(date, date, groupId, cancellationToken);

        return groupId.HasValue
            ? sealedRows.Count > 0
            : sealedRows.Any(row => row.GroupId == null);
    }
}
