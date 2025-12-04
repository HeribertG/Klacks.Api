using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Breaks;

public record ListQuery(BreakFilter Filter) : IRequest<(IEnumerable<ClientBreakResource> Clients, int TotalCount)>;
