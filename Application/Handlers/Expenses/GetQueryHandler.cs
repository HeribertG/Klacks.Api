// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Expenses;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ExpensesResource>, ExpensesResource>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ExpensesResource> Handle(GetQuery<ExpensesResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var expenses = await _expensesRepository.Get(request.Id);

            if (expenses == null)
            {
                throw new KeyNotFoundException($"Expenses with ID {request.Id} not found");
            }

            return _scheduleMapper.ToExpensesResource(expenses);
        }, nameof(Handle), new { request.Id });
    }
}
