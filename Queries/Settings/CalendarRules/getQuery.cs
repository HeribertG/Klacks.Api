using MediatR;

namespace Klacks_api.Queries.Settings.CalendarRules;

public record GetQuery(Guid Id) : IRequest<Models.Settings.CalendarRule>;
