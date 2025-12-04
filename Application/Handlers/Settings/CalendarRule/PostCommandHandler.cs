using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.CalendarRule?>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
                              ISettingsRepository settingsRepository,
                              ScheduleMapper scheduleMapper,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Domain.Models.Settings.CalendarRule?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var calendarRule = _scheduleMapper.ToCalendarRuleEntity(request.model);
            var result = _settingsRepository.AddCalendarRule(calendarRule);

            await _unitOfWork.CompleteAsync();

            return result;
        },
        "operation",
        new { });
    }
}
