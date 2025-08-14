using AutoMapper;
using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.CalendarRule?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              ISettingsRepository settingsRepository,
                              IMapper mapper,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _mapper = mapper;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarRule = _mapper.Map<Klacks.Api.Domain.Models.Settings.CalendarRule>(request.model);
            var result = _settingsRepository.PutCalendarRule(calendarRule);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("CalendarRule with ID {CalendarRuleId} updated successfully.", result.Id);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating CalendarRule with ID {CalendarRuleId}.", request.model.Id);
            throw;
        }
    }
}
