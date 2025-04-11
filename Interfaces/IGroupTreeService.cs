using Klacks.Api.Resources.Associations;

namespace Klacks.Api.Interfaces;

public interface IGroupTreeService
{
    /// <summary>
    /// Erstellt eine neue Gruppe als Kind einer bestehenden Gruppe
    /// </summary>
    Task<GroupTreeNodeResource> CreateGroupNode(Guid? parentId, GroupCreateResource groupResource, string currentUser);

    /// <summary>
    /// Aktualisiert eine bestehende Gruppe
    /// </summary>
    Task<GroupTreeNodeResource> UpdateGroupNode(Guid id, GroupUpdateResource groupResource, string currentUser);

    /// <summary>
    /// Löscht eine Gruppe und all ihre Untergruppen
    /// </summary>
    Task DeleteGroupNode(Guid id, string currentUser);

    /// <summary>
    /// Holt Details zu einer spezifischen Gruppe, einschließlich Mitglieder und Metadaten
    /// </summary>
    Task<GroupTreeNodeResource> GetGroupNodeDetails(Guid id);

    /// <summary>
    /// Erstellt eine Baumdarstellung einer Gruppe und ihrer Untergruppen
    /// </summary>
    Task<GroupTreeResource> GetGroupTree(Guid? rootId = null);

    /// <summary>
    /// Holt den Pfad von der Wurzel bis zum angegebenen Knoten
    /// </summary>
    Task<List<GroupTreeNodeResource>> GetPathToNode(Guid nodeId);
}