using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Handlers.Settings.CalendarRules;

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
