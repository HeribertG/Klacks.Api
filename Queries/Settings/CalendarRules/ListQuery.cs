using MediatR;

namespace Klacks_api.Queries.Settings.CalendarRules;

public record ListQuery : IRequest<IEnumerable<Models.Settings.CalendarRule>>;
