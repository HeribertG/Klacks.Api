using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostCommandHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ExpensesResource?> Handle(PostCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new Expenses");

        var expenses = _scheduleMapper.ToExpensesEntity(request.Resource);
        await _expensesRepository.Add(expenses);
        await _unitOfWork.CompleteAsync();

        var expensesWithWork = await _expensesRepository.Get(expenses.Id);
        if (expensesWithWork?.Work != null)
        {
            var work = expensesWithWork.Work;
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;
            
            // Send ScheduleUpdated for grid refresh
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                work.ClientId, work.CurrentDate, "updated", connectionId, periodStart, periodEnd);
            await _notificationService.NotifyScheduleUpdated(notification);
            
            // Send PeriodHoursUpdated with recalculated hours
            await _periodHoursService.RecalculateAndNotifyAsync(work.ClientId, periodStart, periodEnd, connectionId);
        }

        _logger.LogInformation("Expenses created successfully with ID: {Id}", expenses.Id);
        return _scheduleMapper.ToExpensesResource(expenses);
    }
}
