using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.Groups;

public record UpdateGroupNodeCommand(Guid Id, GroupResource Group) : IRequest<GroupResource>;
