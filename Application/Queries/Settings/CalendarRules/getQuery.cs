using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record GetQuery(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.CalendarRule>;
