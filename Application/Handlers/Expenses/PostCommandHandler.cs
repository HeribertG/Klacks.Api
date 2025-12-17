using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExpensesResource?> Handle(PostCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new Expenses");

        var expenses = _scheduleMapper.ToExpensesEntity(request.Resource);
        await _expensesRepository.Add(expenses);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Expenses created successfully with ID: {Id}", expenses.Id);
        return _scheduleMapper.ToExpensesResource(expenses);
    }
}
