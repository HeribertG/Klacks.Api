using Klacks.Api.Presentation.Resources.Filter;
using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Queries.Works;

public record ListQuery(WorkFilter Filter) : IRequest<IEnumerable<ClientWorkResource>>;
