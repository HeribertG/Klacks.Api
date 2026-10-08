// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IShiftDefaultExpensesApplier: one batch read of the shift templates, then stage-only adds through the
/// expenses repository, so the caller's single save persists Works and expenses together.
/// </summary>
/// <param name="shiftExpensesRepository">Source of the default expenses per shift</param>
/// <param name="expensesRepository">Stages the new Expenses rows (no save)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ShiftDefaultExpensesApplier : IShiftDefaultExpensesApplier
{
    private readonly IShiftExpensesRepository _shiftExpensesRepository;
    private readonly IExpensesRepository _expensesRepository;

    public ShiftDefaultExpensesApplier(
        IShiftExpensesRepository shiftExpensesRepository,
        IExpensesRepository expensesRepository)
    {
        _shiftExpensesRepository = shiftExpensesRepository;
        _expensesRepository = expensesRepository;
    }

    public async Task ApplyAsync(IReadOnlyCollection<Work> works, CancellationToken cancellationToken = default)
    {
        var topLevelWorks = works.Where(w => w.ParentWorkId == null).ToList();
        if (topLevelWorks.Count == 0)
        {
            return;
        }

        var templates = await _shiftExpensesRepository.GetByShiftIdsAsync(
            topLevelWorks.Select(w => w.ShiftId), cancellationToken);
        if (templates.Count == 0)
        {
            return;
        }

        var templatesByShift = templates.ToLookup(t => t.ShiftId);
        foreach (var work in topLevelWorks)
        {
            foreach (var template in templatesByShift[work.ShiftId])
            {
                await _expensesRepository.Add(new Expenses
                {
                    WorkId = work.Id,
                    Amount = template.Amount,
                    Description = template.Description,
                    Taxable = template.Taxable,
                    AnalyseToken = work.AnalyseToken
                });
            }
        }
    }
}
