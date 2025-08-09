using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.Groups;

public record UpdateGroupNodeCommand(Guid Id, GroupResource Group) : IRequest<GroupResource>;
