using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record TruncatedListQuery(CalendarRulesFilter Filter) : IRequest<TruncatedCalendarRule>;
