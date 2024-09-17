using Klacks_api.Models.Settings;
using MediatR;

namespace Klacks_api.Queries.Communications;

public record GetTypeQuery() : IRequest<IEnumerable<CommunicationType>>;
