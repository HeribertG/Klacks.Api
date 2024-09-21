using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Settings.CalendarRules;
using MediatR;

namespace Klacks.Api.Handlers.Settings.CalendarRule
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
