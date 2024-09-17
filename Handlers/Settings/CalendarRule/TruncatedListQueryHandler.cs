using Klacks_api.Interfaces;
using Klacks_api.Queries.Settings.CalendarRules;
using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Handlers.Settings.CalendarRules;

public class TruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedCalendarRule>
{
  private readonly ISettingsRepository repository;

  public TruncatedListQueryHandler(ISettingsRepository repository)
  {
    this.repository = repository;
  }

  public async Task<TruncatedCalendarRule> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
  {
    return await repository.GetTruncatedCalendarRuleList(request.Filter);
  }
}
