// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a single expense entry by id. An expense whose parent Work is owned by a client outside the
/// caller's group visibility is answered exactly like an expense that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Expenses;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ExpensesResource>, ExpensesResource>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IExpensesRepository expensesRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ExpensesResource> Handle(GetQuery<ExpensesResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var expenses = await _expensesRepository.Get(request.Id);

            if (expenses == null
                || (expenses.Work != null
                    && !await _clientVisibilityGuard.IsVisibleAsync(expenses.Work.ClientId, cancellationToken)))
            {
                throw new KeyNotFoundException($"Expenses with ID {request.Id} not found");
            }

            return _scheduleMapper.ToExpensesResource(expenses);
        }, nameof(Handle), new { request.Id });
    }
}
