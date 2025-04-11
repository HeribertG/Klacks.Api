using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.Groups;

/// <summary>
/// Command zum Verschieben einer Gruppe zu einem neuen Elternteil
/// </summary>
public record MoveGroupNodeCommand(Guid NodeId, Guid NewParentId) : IRequest<GroupTreeNodeResource>;
