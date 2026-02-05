using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

public record UpdateGroupNodeCommand(Guid Id, GroupResource Group) : IRequest<GroupResource>;
