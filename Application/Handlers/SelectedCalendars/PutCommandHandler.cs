using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PutCommandHandler : IRequestHandler<PutCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ISelectedCalendarRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              ISelectedCalendarRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<SelectedCalendarResource?> Handle(PutCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbSelectedCalendar = await repository.Get(request.Resource.Id);
            if (dbSelectedCalendar == null)
            {
                logger.LogWarning("SelectedCalendar with ID {Id} not found.", request.Resource.Id);
                return null;
            }

            var updatedSelectedCalendar = mapper.Map(request.Resource, dbSelectedCalendar);
            updatedSelectedCalendar = await repository.Put(updatedSelectedCalendar);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("SelectedCalendar with ID {Id} updated successfully.", request.Resource.Id);

            return mapper.Map<Models.CalendarSelections.SelectedCalendar, SelectedCalendarResource>(updatedSelectedCalendar);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating SelectedCalendar with ID {Id}.", request.Resource.Id);
            throw;
        }
    }
}
