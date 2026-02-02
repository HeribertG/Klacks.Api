using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExpensesResource?> Handle(PutCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating Expenses with ID: {Id}", request.Resource.Id);

        var existingExpenses = await _expensesRepository.GetNoTracking(request.Resource.Id);
        if (existingExpenses == null)
        {
            _logger.LogWarning("Expenses not found: {Id}", request.Resource.Id);
            return null;
        }

        var expenses = _scheduleMapper.ToExpensesEntity(request.Resource);
        var updatedExpenses = await _expensesRepository.Put(expenses);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Expenses updated successfully: {Id}", request.Resource.Id);
        return updatedExpenses != null ? _scheduleMapper.ToExpensesResource(updatedExpenses) : null;
    }
}
