using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Queries.Works;

public record ListQuery(WorkFilter Filter) : IRequest<IEnumerable<ClientWorkResource>>;
