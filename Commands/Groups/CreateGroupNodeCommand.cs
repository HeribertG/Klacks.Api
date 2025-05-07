using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.Groups;

public record CreateGroupNodeCommand(Guid? ParentId, GroupCreateResource Group) : IRequest<GroupResource>;
