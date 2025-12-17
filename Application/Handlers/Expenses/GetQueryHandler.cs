using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class GetQueryHandler : IRequestHandler<GetQuery<ExpensesResource>, ExpensesResource>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<ExpensesResource> Handle(GetQuery<ExpensesResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting Expenses with ID: {Id}", request.Id);

            var expenses = await _expensesRepository.Get(request.Id);

            if (expenses == null)
            {
                throw new KeyNotFoundException($"Expenses with ID {request.Id} not found");
            }

            var result = _scheduleMapper.ToExpensesResource(expenses);
            _logger.LogInformation("Successfully retrieved Expenses with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Expenses with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving Expenses with ID {request.Id}: {ex.Message}");
        }
    }
}
