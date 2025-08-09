using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Queries.Breaks;

public record ListQuery(BreakFilter Filter) : IRequest<IEnumerable<ClientBreakResource>>;
