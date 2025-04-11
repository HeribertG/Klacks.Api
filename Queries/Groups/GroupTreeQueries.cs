using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Queries.Groups;

/// <summary>
/// Query zum Abrufen des Gruppenbaums ausgehend von einer optionalen Wurzel
/// </summary>
public record GetGroupTreeQuery(Guid? RootId) : IRequest<GroupTreeResource>;

/// <summary>
/// Query zum Abrufen der Details einer spezifischen Gruppe im Baum
/// </summary>
public record GetGroupNodeDetailsQuery(Guid Id) : IRequest<GroupTreeNodeResource>;

/// <summary>
/// Query zum Abrufen des Pfades von der Wurzel bis zum angegebenen Knoten
/// </summary>
public record GetPathToNodeQuery(Guid NodeId) : IRequest<List<GroupTreeNodeResource>>;