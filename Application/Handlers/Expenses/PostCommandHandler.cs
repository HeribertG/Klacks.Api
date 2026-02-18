using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;

    public PostCommandHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        ILogger<PostCommandHandler> logger)
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

    public async Task<ExpensesResource?> Handle(PostCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var expenses = _scheduleMapper.ToExpensesEntity(request.Resource);
            await _expensesRepository.Add(expenses);
            await _unitOfWork.CompleteAsync();

            var expensesWithWork = await _expensesRepository.Get(expenses.Id);
            if (expensesWithWork?.Work != null)
            {
                var work = expensesWithWork.Work;
                await _scheduleChangeTracker.TrackChangeAsync(work.ClientId, work.CurrentDate);
                var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
                var connectionId = _httpContextAccessor.HttpContext?.Request
                    .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;

                var notification = _scheduleMapper.ToScheduleNotificationDto(
                    work.ClientId, work.CurrentDate, "updated", connectionId, periodStart, periodEnd);
                await _notificationService.NotifyScheduleUpdated(notification);

                await _periodHoursService.RecalculateAndNotifyAsync(work.ClientId, periodStart, periodEnd, connectionId);
            }

            return _scheduleMapper.ToExpensesResource(expenses);
        }, "CreateExpenses", new { request.Resource.Id });
    }
}
