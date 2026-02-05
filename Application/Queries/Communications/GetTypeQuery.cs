using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Communications;

public record GetTypeQuery() : IRequest<IEnumerable<CommunicationTypeResource>>;
