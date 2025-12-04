using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Works;

public record ListQuery(WorkFilter Filter) : IRequest<IEnumerable<ClientWorkResource>>;
