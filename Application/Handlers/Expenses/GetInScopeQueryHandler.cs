// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles GetExpenseInScopeQuery: loads the expense in any scope, then answers it only when its parent Work's
/// AnalyseToken (the scope is the Work's, so legacy rows with a drifted own token still resolve correctly) equals the
/// requested scope and its owner (the parent Work's client) is visible to the caller. Every other case
/// - missing, other scope, hidden owner - throws the same KeyNotFoundException, so the scope is no existence oracle.
/// </summary>
/// <param name="expensesRepository">Loads the expense with its parent Work regardless of scope</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Expenses;

public class GetInScopeQueryHandler : BaseHandler, IRequestHandler<GetExpenseInScopeQuery, ExpensesResource>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetInScopeQueryHandler(
        IExpensesRepository expensesRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetInScopeQueryHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ExpensesResource> Handle(GetExpenseInScopeQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var expenses = await _expensesRepository.GetWithWorkInAnyScope(request.Id);

            if (expenses?.Work == null
                || expenses.Work.AnalyseToken != request.AnalyseToken
                || !await _clientVisibilityGuard.IsVisibleAsync(expenses.Work.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Expenses with ID {request.Id} not found");
            }

            return _scheduleMapper.ToExpensesResource(expenses);
        }, nameof(Handle), new { request.Id });
    }
}
