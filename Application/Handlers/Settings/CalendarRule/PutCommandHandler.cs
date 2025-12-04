using AutoMapper;
using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand, Domain.Models.Settings.CalendarRule?>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
                              ISettingsRepository settingsRepository,
                              IMapper mapper,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<Domain.Models.Settings.CalendarRule?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var calendarRule = _mapper.Map<Domain.Models.Settings.CalendarRule>(request.model);
            var result = _settingsRepository.PutCalendarRule(calendarRule);
            await _unitOfWork.CompleteAsync();

            return result;
        },
        "operation",
        new { });
    }
}
