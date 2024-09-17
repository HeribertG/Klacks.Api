using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.CalendarSelections
{
  public class GetListQueryHandler : IRequestHandler<ListQuery<CalendarSelectionResource>, IEnumerable<CalendarSelectionResource>>
  {
    private readonly IMapper mapper;
    private readonly ICalendarSelectionRepository repository;

    public GetListQueryHandler(
          IMapper mapper,
          ICalendarSelectionRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<CalendarSelectionResource>> Handle(ListQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
      var calendarSelection = await this.repository.List();
      return this.mapper.Map<List<Models.CalendarSelections.CalendarSelection>, List<CalendarSelectionResource>>(calendarSelection!);
    }
  }
}
