using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Queries.Communications;

public record GetTypeQuery() : IRequest<IEnumerable<CommunicationTypeResource>>;
