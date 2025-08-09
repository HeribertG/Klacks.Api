using MediatR;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record ListQuery : IRequest<IEnumerable<Models.Settings.CalendarRule>>;
