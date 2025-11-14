using Klacks.Api.Presentation.DTOs.Dashboard;
using MediatR;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetClientLocationsQuery : IRequest<IEnumerable<ClientLocationResource>>;
