using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Commands.Groups;

public record CreateGroupNodeCommand(Guid? ParentId, GroupResource Group) : IRequest<GroupResource>;
