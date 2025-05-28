using MediatR;

namespace Klacks.Api.Commands.Groups;

public record CreateGroupNodeCommand(Guid? ParentId, GroupResource Group) : IRequest<GroupResource>;
