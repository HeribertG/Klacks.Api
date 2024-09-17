using Klacks_api.Resources.Filter;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Queries.Works;

public record ListQuery(WorkFilter Filter) : IRequest<IEnumerable<ClientWorkResource>>;
