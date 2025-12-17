using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<ExpensesResource>, IEnumerable<ExpensesResource>>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<ExpensesResource>> Handle(ListQuery<ExpensesResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all Expenses");

        var expenses = await _expensesRepository.List();

        _logger.LogInformation("Successfully retrieved {Count} Expenses", expenses.Count);
        return _scheduleMapper.ToExpensesResourceList(expenses);
    }
}
