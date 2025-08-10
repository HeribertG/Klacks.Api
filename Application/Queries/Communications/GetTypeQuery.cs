using Klacks.Api.Domain.Models.Settings;
using MediatR;

namespace Klacks.Api.Application.Queries.Communications;

public record GetTypeQuery() : IRequest<IEnumerable<CommunicationType>>;
