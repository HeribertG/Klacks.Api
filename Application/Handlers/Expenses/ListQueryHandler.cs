// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists Expenses of exactly one scope. ListQuery&lt;ExpensesResource&gt; (REST GET /Expenses) lists the main plan only;
/// ListExpensesInScopeQuery lists the main plan or one scenario - scenario rows never leak into the main-plan list
/// and vice versa. Only entries whose parent Work is owned by a client inside the caller's group visibility are
/// returned; an entry whose parent Work cannot be resolved is left out as well.
/// </summary>
/// <param name="workRepository">Resolves the owning client of each parent Work in one query</param>
/// <param name="clientVisibilityGuard">Filters the entries down to clients the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class ListQueryHandler :
    BaseHandler,
    IRequestHandler<ListQuery<ExpensesResource>, IEnumerable<ExpensesResource>>,
    IRequestHandler<ListExpensesInScopeQuery, IEnumerable<ExpensesResource>>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IExpensesRepository expensesRepository,
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public Task<IEnumerable<ExpensesResource>> Handle(ListQuery<ExpensesResource> request, CancellationToken cancellationToken)
        => ListInScopeAsync(null, cancellationToken);

    public Task<IEnumerable<ExpensesResource>> Handle(ListExpensesInScopeQuery request, CancellationToken cancellationToken)
        => ListInScopeAsync(request.AnalyseToken, cancellationToken);

    private async Task<IEnumerable<ExpensesResource>> ListInScopeAsync(Guid? analyseToken, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting Expenses of scope {AnalyseToken}", analyseToken);

        var allExpenses = await _expensesRepository.ListInScopeAsync(analyseToken, cancellationToken);
        var ownerByWorkId = (await _workRepository.GetByIdsAsync(allExpenses.Select(e => e.WorkId).Distinct()))
            .ToDictionary(w => w.Id, w => w.ClientId);
        var expenses = await _clientVisibilityGuard.FilterVisibleAsync(
            allExpenses.Where(e => ownerByWorkId.ContainsKey(e.WorkId)).ToList(),
            e => ownerByWorkId[e.WorkId],
            cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} Expenses", expenses.Count);
        return _scheduleMapper.ToExpensesResourceList(expenses);
    }
}
