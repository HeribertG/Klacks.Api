using MediatR;

namespace Klacks.Api.Queries.Settings.CalendarRules;

public record GetQuery(Guid Id) : IRequest<Models.Settings.CalendarRule>;
