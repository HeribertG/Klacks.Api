using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Settings.CalendarRules;
using MediatR;

namespace Klacks_api.Handlers.Settings.CalendarRule
{
  public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Models.Settings.CalendarRule>>
  {
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;

    public ListQueryHandler(IMapper mapper, ISettingsRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<Models.Settings.CalendarRule>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
      return await repository.GetCalendarRuleList();
    }
  }
}
