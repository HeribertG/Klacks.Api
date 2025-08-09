using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record TruncatedListQuery(CalendarRulesFilter Filter) : IRequest<TruncatedCalendarRule>;
