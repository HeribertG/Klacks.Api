using MediatR;

namespace Klacks.Api.Queries.Settings.CalendarRules;

public record ListQuery : IRequest<IEnumerable<Models.Settings.CalendarRule>>;
