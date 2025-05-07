using MediatR;

namespace Klacks.Api.Commands.Groups;


public record MoveGroupNodeCommand(Guid NodeId, Guid NewParentId) : IRequest<GroupResource>;
