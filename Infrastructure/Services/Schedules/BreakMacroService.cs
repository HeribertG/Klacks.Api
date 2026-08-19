// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Processes macro calculations for break entries (working time from break macros).
/// </summary>
/// <param name="context">Database access for absence and break data</param>
/// <param name="macroDataProvider">Calculates macro input data from break entries</param>
/// <param name="macroCompilationService">Compiles and executes break macros</param>
/// <param name="logger">Logger for warnings and error messages</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class BreakMacroService : IBreakMacroService
{
    private readonly DataBaseContext _context;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroCompilationService _macroCompilationService;
    private readonly ILogger<BreakMacroService> _logger;

    public BreakMacroService(
        DataBaseContext context,
        IMacroDataProvider macroDataProvider,
        IMacroCompilationService macroCompilationService,
        ILogger<BreakMacroService> logger)
    {
        _context = context;
        _macroDataProvider = macroDataProvider;
        _macroCompilationService = macroCompilationService;
        _logger = logger;
    }

    /// <summary>
    /// True when the entry carries a directly recorded duration instead of a time span.
    /// </summary>
    /// <remarks>
    /// Absence details can be recorded either as a time range or as a plain duration. In the
    /// duration case no times exist, so both bounds are equal ("no times recorded") and the hours
    /// live in WorkTime. Deriving a duration from those bounds would discard what the user entered,
    /// which is why the macro must not overwrite it. Equal bounds with a WorkTime of zero are a
    /// genuinely empty entry and still go through the macro.
    /// </remarks>
    private static bool HasDirectlyRecordedDuration(Break breakEntry)
        => breakEntry.StartTime == breakEntry.EndTime && breakEntry.WorkTime > 0;

    public async Task ProcessBreakMacroAsync(Break breakEntry, int? paymentInterval = null)
    {
        try
        {
            if (HasDirectlyRecordedDuration(breakEntry))
            {
                return;
            }

            var absence = await _context.Absence.FirstOrDefaultAsync(a => a.Id == breakEntry.AbsenceId);
            if (absence?.MacroId == null || absence.MacroId.Value == Guid.Empty)
            {
                return;
            }

            var macroData = await _macroDataProvider.GetMacroDataForBreakAsync(breakEntry, paymentInterval);
            var result = await _macroCompilationService.CompileAndExecuteAsync(absence.MacroId.Value, macroData);

            if (result.Success && result.ResultValue.HasValue)
            {
                breakEntry.WorkTime = result.ResultValue.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for Break {BreakId}", breakEntry.Id);
        }
    }

    public async Task ReprocessAllBreaksAsync(DateOnly startDate, DateOnly endDate, List<Guid>? clientIds = null)
    {
        var query = _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate);

        if (clientIds is { Count: > 0 })
        {
            query = query.Where(b => clientIds.Contains(b.ClientId));
        }

        var sealedCount = await query.CountAsync(b => b.LockLevel != WorkLockLevel.None);
        if (sealedCount > 0)
        {
            _logger.LogWarning(
                "Skipping {Count} sealed break(s) (LockLevel != None) in {Start}..{End}; their work time stays unchanged until they are unsealed and reprocessed",
                sealedCount,
                startDate,
                endDate);
        }

        var breaks = await query.Where(b => b.LockLevel == WorkLockLevel.None).ToListAsync();

        _logger.LogInformation("Reprocessing macros for {Count} breaks from {Start} to {End}", breaks.Count, startDate, endDate);

        foreach (var breakEntry in breaks)
        {
            await ProcessBreakMacroAsync(breakEntry);
        }

        await _context.SaveChangesAsync();
    }
}
