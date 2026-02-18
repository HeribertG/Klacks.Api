using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;

    public PutCommandHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
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

        if (updatedExpenses != null)
        {
            var expensesWithWork = await _expensesRepository.Get(updatedExpenses.Id);
            if (expensesWithWork?.Work != null)
            {
                var work = expensesWithWork.Work;
                await _scheduleChangeTracker.TrackChangeAsync(work.ClientId, work.CurrentDate);
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
        }

        _logger.LogInformation("Expenses updated successfully: {Id}", request.Resource.Id);
        return updatedExpenses != null ? _scheduleMapper.ToExpensesResource(updatedExpenses) : null;
    }
}
