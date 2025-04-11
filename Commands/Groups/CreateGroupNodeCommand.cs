using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.Groups;

/// <summary>
/// Command zum Erstellen einer neuen Gruppe als Kind einer bestehenden Gruppe oder als Wurzel
/// </summary>
public record CreateGroupNodeCommand(Guid? ParentId, GroupCreateResource Group) : IRequest<GroupTreeNodeResource>;
