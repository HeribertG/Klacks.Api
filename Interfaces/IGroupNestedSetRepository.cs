using Klacks.Api.Models.Associations;

namespace Klacks.Api.Interfaces;

public interface IGroupNestedSetRepository
{
    /// <summary>
    /// Fügt eine neue Gruppe als Kind einer bestehenden Gruppe hinzu
    /// </summary>
    Task<Group> AddChildNode(Guid parentId, Group newGroup);

    /// <summary>
    /// Fügt eine neue Wurzelgruppe hinzu
    /// </summary>
    Task<Group> AddRootNode(Group newGroup);

    /// <summary>
    /// Löscht eine Gruppe und all ihre Untergruppen
    /// </summary>
    Task DeleteNode(Guid id);

    /// <summary>
    /// Verschiebt einen Knoten zu einem neuen Elternteil
    /// </summary>
    Task MoveNode(Guid nodeId, Guid newParentId);

    /// <summary>
    /// Holt alle direkten Untergruppen einer Gruppe
    /// </summary>
    Task<IEnumerable<Group>> GetChildren(Guid parentId);

    /// <summary>
    /// Holt den kompletten Baum von einer bestimmten Wurzel aus
    /// </summary>
    Task<IEnumerable<Group>> GetTree(Guid? rootId = null);

    /// <summary>
    /// Holt den Pfad von der Wurzel bis zum angegebenen Knoten
    /// </summary>
    Task<IEnumerable<Group>> GetPath(Guid nodeId);

    /// <summary>
    /// Aktualisiert die Informationen einer Gruppe (ohne die Baumstruktur zu ändern)
    /// </summary>
    Task UpdateNode(Group updatedGroup);

    /// <summary>
    /// Berechnet die Tiefe eines Knotens im Baum
    /// </summary>
    Task<int> GetNodeDepth(Guid nodeId);
}