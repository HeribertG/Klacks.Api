using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Commands.Groups;


public record MoveGroupNodeCommand(Guid NodeId, Guid NewParentId) : IRequest<GroupResource>;
