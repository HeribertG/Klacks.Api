using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.CalendarSelections
{
  public class GetQueryHandler : IRequestHandler<GetQuery<CalendarSelectionResource>, CalendarSelectionResource?>
  {
    private readonly IMapper mapper;
    private readonly ICalendarSelectionRepository repository;

    public GetQueryHandler(IMapper mapper, ICalendarSelectionRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<CalendarSelectionResource?> Handle(GetQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
      var calendarSelection = await repository.Get(request.Id);
      return mapper.Map<Models.CalendarSelections.CalendarSelection, CalendarSelectionResource>(calendarSelection!);
    }
  }
}
