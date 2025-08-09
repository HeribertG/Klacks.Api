using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetPathToNodeQuery(Guid NodeId) : IRequest<List<GroupResource>>;
