// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Verarbeitet Macro-Berechnungen für Break-Einträge (Arbeitszeit aus Pausenmakros).
/// </summary>
/// <param name="context">Datenbankzugriff für Absence- und Break-Daten</param>
/// <param name="macroDataProvider">Berechnet Macro-Eingabedaten aus Break-Einträgen</param>
/// <param name="macroCompilationService">Kompiliert und führt Macros aus</param>
/// <param name="logger">Logger für Warn- und Fehlermeldungen</param>

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

    public async Task ProcessBreakMacroAsync(Break breakEntry, int? paymentInterval = null)
    {
        try
        {
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

        var breaks = await query.ToListAsync();

        _logger.LogInformation("Reprocessing macros for {Count} breaks from {Start} to {End}", breaks.Count, startDate, endDate);

        foreach (var breakEntry in breaks)
        {
            await ProcessBreakMacroAsync(breakEntry);
        }

        await _context.SaveChangesAsync();
    }
}
