using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class PostCommandHandler : IRequestHandler<PostCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ICalendarSelectionRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              ICalendarSelectionRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<CalendarSelectionResource?> Handle(PostCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarSelection = mapper.Map<CalendarSelectionResource, Models.CalendarSelections.CalendarSelection>(request.Resource);
            await repository.Add(calendarSelection);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New CalendarSelection added successfully. ID: {Id}", calendarSelection.Id);

            return mapper.Map<Models.CalendarSelections.CalendarSelection, CalendarSelectionResource>(calendarSelection);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new CalendarSelection. ID: {Id}", request.Resource.Id);
            throw;
        }
    }
}
