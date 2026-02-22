using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand, Domain.Models.Settings.CalendarRule>
{    
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
                                ISettingsRepository settingsRepository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Domain.Models.Settings.CalendarRule> Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var calendarRule = await _settingsRepository.DeleteCalendarRule(request.Id);
            if (calendarRule == null)
            {
                return null!;
            }

            await _unitOfWork.CompleteAsync();

            return calendarRule;
        }, 
        "operation", 
        new { });
    }
}
