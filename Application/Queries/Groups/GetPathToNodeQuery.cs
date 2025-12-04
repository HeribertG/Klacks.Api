using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Groups;

public record GetPathToNodeQuery(Guid NodeId) : IRequest<List<GroupResource>>;
