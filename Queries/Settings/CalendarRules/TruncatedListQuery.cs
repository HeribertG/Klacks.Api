using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Queries.Settings.CalendarRules;

public record TruncatedListQuery(CalendarRulesFilter Filter) : IRequest<TruncatedCalendarRule>;
