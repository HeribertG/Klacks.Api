using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Queries.Works;

public record ListQuery(WorkFilter Filter) : IRequest<IEnumerable<ClientWorkResource>>;
