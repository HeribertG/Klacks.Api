using Klacks.Api.Presentation.Resources.Filter;
using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Queries.Breaks;

public record ListQuery(BreakFilter Filter) : IRequest<IEnumerable<ClientBreakResource>>;
