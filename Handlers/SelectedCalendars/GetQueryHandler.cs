using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.SelectedCalendars
{
  public class GetQueryHandler : IRequestHandler<GetQuery<SelectedCalendarResource>, SelectedCalendarResource?>
  {
    private readonly IMapper mapper;
    private readonly ISelectedCalendarRepository repository;

    public GetQueryHandler(IMapper mapper, ISelectedCalendarRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<SelectedCalendarResource?> Handle(GetQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
      var selectedCalendar = await repository.Get(request.Id);
      return mapper.Map<Models.CalendarSelections.SelectedCalendar, SelectedCalendarResource>(selectedCalendar!);
    }
  }
}
