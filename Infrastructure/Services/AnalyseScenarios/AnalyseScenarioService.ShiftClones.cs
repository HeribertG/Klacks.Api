// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Clones the shift-scoped satellites of a scenario (client shift preferences, required qualifications and
/// shift expenses) onto the cloned shifts. Split out of AnalyseScenarioService.cs to keep that file under its
/// size ceiling; behaviour unchanged.
/// </summary>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    private async Task CloneClientShiftPreferences(Dictionary<Guid, Guid> shiftIdMap, Guid token, ScenarioCloneSource source, CancellationToken ct)
    {
        if (shiftIdMap.Count == 0) return;

        var sourceShiftIds = shiftIdMap.Keys.ToList();
        var preferences = await WhereEitherWorld(_context.Set<ClientShiftPreference>()
            .Where(p => !p.IsDeleted
                && sourceShiftIds.Contains(p.ShiftId)), source)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var preference in preferences)
        {
            var clone = new ClientShiftPreference
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ClientId = preference.ClientId,
                ShiftId = shiftIdMap[preference.ShiftId],
                PreferenceType = preference.PreferenceType
            };

            await _context.Set<ClientShiftPreference>().AddAsync(clone, ct);
        }
    }

    private async Task CloneShiftRequiredQualifications(Dictionary<Guid, Guid> shiftIdMap, Guid token, CancellationToken ct)
    {
        if (shiftIdMap.Count == 0) return;

        var sourceShiftIds = shiftIdMap.Keys.ToList();
        var requirements = await _context.ShiftRequiredQualification.IgnoreQueryFilters()
            .Where(srq => !srq.IsDeleted && sourceShiftIds.Contains(srq.ShiftId))
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var req in requirements)
        {
            var clone = new ShiftRequiredQualification
            {
                Id = Guid.NewGuid(),
                ShiftId = shiftIdMap[req.ShiftId],
                QualificationId = req.QualificationId,
                IsMandatory = req.IsMandatory,
                MinLevel = req.MinLevel
            };

            await _context.ShiftRequiredQualification.AddAsync(clone, ct);
        }
    }

    private async Task CloneShiftExpenses(Dictionary<Guid, Guid> shiftIdMap, Guid token, ScenarioCloneSource source, CancellationToken ct)
    {
        if (shiftIdMap.Count == 0) return;

        var sourceShiftIds = shiftIdMap.Keys.ToList();
        var expenses = await WhereEitherWorld(_context.Set<ShiftExpenses>()
            .Where(e => !e.IsDeleted
                && sourceShiftIds.Contains(e.ShiftId)), source)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var expense in expenses)
        {
            var clone = new ShiftExpenses
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                ShiftId = shiftIdMap[expense.ShiftId],
                Amount = expense.Amount,
                Description = expense.Description,
                Taxable = expense.Taxable
            };

            await _context.Set<ShiftExpenses>().AddAsync(clone, ct);
        }
    }
}
