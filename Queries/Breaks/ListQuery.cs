using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Queries.Breaks;

public record ListQuery(BreakFilter Filter) : IRequest<IEnumerable<ClientBreakResource>>;
