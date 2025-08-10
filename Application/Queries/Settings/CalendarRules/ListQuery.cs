using MediatR;

namespace Klacks.Api.Application.Queries.Settings.CalendarRules;

public record ListQuery : IRequest<IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>>;
