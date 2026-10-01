// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists Expenses. Only entries whose parent Work is owned by a client inside the caller's group visibility
/// are returned; an entry whose parent Work cannot be resolved is left out as well.
/// </summary>
/// <param name="workRepository">Resolves the owning client of each parent Work in one query</param>
/// <param name="clientVisibilityGuard">Filters the entries down to clients the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<ExpensesResource>, IEnumerable<ExpensesResource>>
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

    public async Task<IEnumerable<ExpensesResource>> Handle(ListQuery<ExpensesResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all Expenses");

        var allExpenses = await _expensesRepository.List();
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
