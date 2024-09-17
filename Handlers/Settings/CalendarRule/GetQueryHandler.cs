using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Settings.CalendarRules;
using MediatR;

namespace Klacks_api.Handlers.Settings.CalendarRule
{
  public class GetQueryHandler : IRequestHandler<GetQuery, Models.Settings.CalendarRule?>
  {
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;

    public GetQueryHandler(IMapper mapper, ISettingsRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<Models.Settings.CalendarRule?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
      return await repository.GetCalendarRule(request.Id);
    }
  }
}
