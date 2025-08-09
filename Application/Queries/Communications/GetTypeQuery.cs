using Klacks.Api.Models.Settings;
using MediatR;

namespace Klacks.Api.Queries.Communications;

public record GetTypeQuery() : IRequest<IEnumerable<CommunicationType>>;
