// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a single expense entry by id in whatever scope it is stored: the scope comes from the stored row and its parent
/// Work, because the edit dialog of a scenario addresses the row by id only. An expense whose parent Work owner or
/// scenario is hidden from the caller is answered exactly like an expense that does not exist.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client and the scenario of the expense</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Expenses;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ExpensesResource>, ExpensesResource>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IExpensesRepository expensesRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ExpensesResource> Handle(GetQuery<ExpensesResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var expenses = await _expensesRepository.GetWithWorkInAnyScope(request.Id);

            if (expenses == null
                || !await _rowAccessGuard.AreAllVisibleAsync(
                    expenses.Work == null ? [] : [expenses.Work.ClientId],
                    expenses.Work?.AnalyseToken ?? expenses.AnalyseToken,
                    cancellationToken))
            {
                throw new KeyNotFoundException($"Expenses with ID {request.Id} not found");
            }

            return _scheduleMapper.ToExpensesResource(expenses);
        }, nameof(Handle), new { request.Id });
    }
}
