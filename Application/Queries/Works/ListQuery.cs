using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Queries.Works;

public record ListQuery(WorkFilter Filter) : IRequest<IEnumerable<ClientWorkResource>>;
