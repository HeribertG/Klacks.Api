using Klacks.Api.Presentation.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetClientLocationsQuery : IRequest<IEnumerable<ClientLocationResource>>;
