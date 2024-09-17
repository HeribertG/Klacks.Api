using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.SelectedCalendars
{
  public class GetListQueryHandler : IRequestHandler<ListQuery<SelectedCalendarResource>, IEnumerable<SelectedCalendarResource>>
  {
    private readonly IMapper mapper;
    private readonly ISelectedCalendarRepository repository;

    public GetListQueryHandler(
          IMapper mapper,
          ISelectedCalendarRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<SelectedCalendarResource>> Handle(ListQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
      var selectedCalendar = await this.repository.List();
      return this.mapper.Map<List<Models.CalendarSelections.SelectedCalendar>, List<SelectedCalendarResource>>(selectedCalendar!);
    }
  }
}
