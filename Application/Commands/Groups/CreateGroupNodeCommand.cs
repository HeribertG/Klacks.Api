using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

public record CreateGroupNodeCommand(Guid? ParentId, GroupResource Group) : IRequest<GroupResource>;
