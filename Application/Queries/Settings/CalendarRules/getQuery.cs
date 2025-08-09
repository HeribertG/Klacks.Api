using MediatR;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record GetQuery(Guid Id) : IRequest<Models.Settings.CalendarRule>;
