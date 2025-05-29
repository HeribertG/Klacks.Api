using AutoMapper;
using Klacks.Api.Commands.Settings.CalendarRules;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Handlers.Settings.CalendarRule;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Models.Settings.CalendarRule>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                ISettingsRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Models.Settings.CalendarRule> Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarRule = await repository.DeleteCalendarRule(request.Id);
            if (calendarRule == null)
            {
                logger.LogWarning("CalendarRule with ID {CalendarRuleId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("CalendarRule with ID {CalendarRuleId} deleted successfully.", request.Id);

            return calendarRule;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting CalendarRule with ID {CalendarRuleId}.", request.Id);
            throw;
        }
    }
}
