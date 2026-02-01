using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.Queries.Clients;

public sealed record GetClientsForReplacementQuery() : IRequest<IEnumerable<ClientForReplacementResource>>;
