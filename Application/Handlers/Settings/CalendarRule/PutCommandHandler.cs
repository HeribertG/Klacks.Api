using AutoMapper;
using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PutCommandHandler : IRequestHandler<PutCommand, Models.Settings.CalendarRule?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              ISettingsRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Models.Settings.CalendarRule?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var dbCalendarRule = await repository.GetCalendarRule(request.model.Id);
            if (dbCalendarRule == null)
            {
                logger.LogWarning("CalendarRule with ID {CalendarRuleId} not found.", request.model.Id);
                return null;
            }

            var updatedCalendarRule = mapper.Map(request.model, dbCalendarRule);
            updatedCalendarRule = repository.PutCalendarRule(updatedCalendarRule);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("CalendarRule with ID {CalendarRuleId} updated successfully.", request.model.Id);

            return updatedCalendarRule;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating CalendarRule with ID {CalendarRuleId}.", request.model.Id);
            throw;
        }
    }
}
