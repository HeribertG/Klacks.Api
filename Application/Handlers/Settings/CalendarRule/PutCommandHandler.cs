using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.CalendarRule?>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
                              ISettingsRepository settingsRepository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Domain.Models.Settings.CalendarRule?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var result = _settingsRepository.PutCalendarRule(request.model);
            await _unitOfWork.CompleteAsync();

            return result;
        },
        "operation",
        new { });
    }
}
