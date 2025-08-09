using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetPathToNodeQuery(Guid NodeId) : IRequest<List<GroupResource>>;
