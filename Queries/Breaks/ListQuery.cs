using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Queries.Breaks;

public record ListQuery(BreakFilter Filter) : IRequest<IEnumerable<ClientBreakResource>>;
