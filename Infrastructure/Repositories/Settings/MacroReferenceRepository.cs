// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IMacroReferenceRepository"/>. Holders, cut groups and macros are read untracked; the soft-delete
/// query filters hide deleted rows. The cut group of a key holds every live, non-scenario shift cut or copied from that
/// order (OriginalId equals the key) or being the key itself, never the sealed order row — the same group the shift edit
/// skill propagates metadata over, without the immutable order. A switch loads exactly one shift or absence type row
/// tracked and without navigation properties and sets its MacroId, so only that column (plus the update audit fields the
/// context stamps on save) is written — never a full-graph update, never the sealed-order handling of the shift edit path,
/// never a macro row. Stage-only. An absence type is named by its first non-empty core-language name, else by any other
/// stored name (ordered by language code), so an installation that only fills another language still gets a name.
/// </summary>
/// <param name="context">Database context providing shifts, absence types and macros</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class MacroReferenceRepository : IMacroReferenceRepository
{
    private readonly DataBaseContext _context;

    public MacroReferenceRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<MacroReferenceHolder?> FindHolderAsync(
        MacroAssignmentTarget target, Guid holderId, CancellationToken cancellationToken = default)
    {
        if (target == MacroAssignmentTarget.Shift)
        {
            return await _context.Shift
                .AsNoTracking()
                .Where(s => s.Id == holderId)
                .Select(s => new MacroReferenceHolder(
                    s.Id,
                    MacroAssignmentTarget.Shift,
                    s.Name,
                    s.MacroId,
                    (ShiftStatus?)s.Status,
                    s.AnalyseToken != null || s.ScenarioSourceShiftId != null,
                    s.OriginalId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        var absence = await _context.Absence
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == holderId, cancellationToken);
        return absence == null
            ? null
            : new MacroReferenceHolder(
                absence.Id, MacroAssignmentTarget.AbsenceType, DisplayName(absence.Name), absence.MacroId, null, false, null);
    }

    public async Task<IReadOnlyList<MacroReferenceHolder>> FindCutGroupAsync(
        Guid cutGroupKey, CancellationToken cancellationToken = default)
    {
        return await _context.Shift
            .AsNoTracking()
            .Where(s => (s.OriginalId == cutGroupKey || s.Id == cutGroupKey)
                        && s.Status != ShiftStatus.SealedOrder
                        && s.AnalyseToken == null
                        && s.ScenarioSourceShiftId == null)
            .OrderBy(s => s.Lft)
            .ThenBy(s => s.FromDate)
            .ThenBy(s => s.StartShift)
            .ThenBy(s => s.Id)
            .Select(s => new MacroReferenceHolder(
                s.Id,
                MacroAssignmentTarget.Shift,
                s.Name,
                s.MacroId,
                (ShiftStatus?)s.Status,
                false,
                s.OriginalId))
            .ToListAsync(cancellationToken);
    }

    public async Task<MacroSnapshot?> FindMacroAsync(Guid macroId, CancellationToken cancellationToken = default)
    {
        return await _context.Macro
            .AsNoTracking()
            .Where(m => m.Id == macroId)
            .Select(m => new MacroSnapshot(m.Id, m.Name, m.Type, m.Category, m.Origin, m.Content))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SetMacroIdAsync(
        MacroAssignmentTarget target, Guid holderId, Guid? macroId, CancellationToken cancellationToken = default)
    {
        if (target == MacroAssignmentTarget.Shift)
        {
            var shift = await _context.Shift.FirstOrDefaultAsync(s => s.Id == holderId, cancellationToken);
            if (shift == null)
            {
                return false;
            }

            shift.MacroId = macroId;
            return true;
        }

        var absence = await _context.Absence.FirstOrDefaultAsync(a => a.Id == holderId, cancellationToken);
        if (absence == null)
        {
            return false;
        }

        absence.MacroId = macroId;
        return true;
    }

    private static string DisplayName(MultiLanguage? name) =>
        MultiLanguage.CoreLanguages
            .Select(language => name?.GetValue(language))
            .Concat(AllValuesByLanguage(name))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static IEnumerable<string?> AllValuesByLanguage(MultiLanguage? name) =>
        (name?.GetAllValues() ?? new Dictionary<string, string?>())
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => entry.Value);
}
