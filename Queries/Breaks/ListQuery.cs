using Klacks_api.Resources.Filter;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Queries.Breaks;

public record ListQuery(BreakFilter Filter) : IRequest<IEnumerable<ClientBreakResource>>;
