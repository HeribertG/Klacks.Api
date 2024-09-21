using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Settings.CalendarRules;

public record TruncatedListQuery(CalendarRulesFilter Filter) : IRequest<TruncatedCalendarRule>;
