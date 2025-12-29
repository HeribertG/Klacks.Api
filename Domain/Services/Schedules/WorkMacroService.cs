using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Domain.Services.Schedules;

public class WorkMacroService : IWorkMacroService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMacroManagementService _macroManagementService;
    private readonly IMacroEngine _macroEngine;
    private readonly ILogger<WorkMacroService> _logger;

    public WorkMacroService(
        IShiftRepository shiftRepository,
        IMacroManagementService macroManagementService,
        IMacroEngine macroEngine,
        ILogger<WorkMacroService> logger)
    {
        _shiftRepository = shiftRepository;
        _macroManagementService = macroManagementService;
        _macroEngine = macroEngine;
        _logger = logger;
    }

    public async Task ProcessWorkMacroAsync(Work work)
    {
        var shift = await _shiftRepository.Get(work.ShiftId);
        if (shift == null)
        {
            _logger.LogWarning("Shift with ID {ShiftId} not found for Work {WorkId}", work.ShiftId, work.Id);
            return;
        }

        CalculateWorkTime(work);

        if (!shift.MacroId.HasValue || shift.MacroId.Value == Guid.Empty)
        {
            return;
        }

        var macro = await _macroManagementService.GetMacroAsync(shift.MacroId.Value);

        _macroEngine.ResetImports();
        _macroEngine.PrepareMacro(macro.Id, macro.Content);

        _macroEngine.ImportItem("work", work);
        _macroEngine.ImportItem("shift", shift);

        var results = _macroEngine.Run();

        if (_macroEngine.ErrorNumber != 0)
        {
            _logger.LogError("Macro execution failed for Work {WorkId}: {ErrorCode}", work.Id, _macroEngine.ErrorCode);
        }
    }

    private static void CalculateWorkTime(Work work)
    {
        var start = work.StartTime.ToTimeSpan();
        var end = work.EndTime.ToTimeSpan();

        TimeSpan duration;
        if (end >= start)
        {
            duration = end - start;
        }
        else
        {
            duration = TimeSpan.FromHours(24) - start + end;
        }

        work.WorkTime = (decimal)duration.TotalHours;
    }
}
