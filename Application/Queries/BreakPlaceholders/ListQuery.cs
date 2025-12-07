using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.BreakPlaceholders;

public record ListQuery(BreakFilter Filter) : IRequest<(IEnumerable<ClientBreakPlaceholderResource> Clients, int TotalCount)>;
