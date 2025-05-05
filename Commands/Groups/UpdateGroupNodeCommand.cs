using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.Groups;

// <summary>
/// Command zum Aktualisieren einer bestehenden Gruppe
/// </summary>
public record UpdateGroupNodeCommand(Guid Id, GroupUpdateResource Group) : IRequest<GroupResource>;
