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
    private async Task CloneClientShiftPreferences(Dictionary<Guid, Guid> shiftIdMap, Guid token, CancellationToken ct)
    {
        if (shiftIdMap.Count == 0) return;

        var sourceShiftIds = shiftIdMap.Keys.ToList();
        var preferences = await _context.Set<ClientShiftPreference>()
            .Where(p => !p.IsDeleted
                && p.AnalyseToken == null
                && sourceShiftIds.Contains(p.ShiftId))
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

    private async Task CloneShiftExpenses(Dictionary<Guid, Guid> shiftIdMap, Guid token, CancellationToken ct)
    {
        if (shiftIdMap.Count == 0) return;

        var sourceShiftIds = shiftIdMap.Keys.ToList();
        var expenses = await _context.Set<ShiftExpenses>()
            .Where(e => !e.IsDeleted
                && e.AnalyseToken == null
                && sourceShiftIds.Contains(e.ShiftId))
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
