using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Resources.Associations;
using Klacks.Api.Resources.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Services
{
    public class GroupTreeService : IGroupTreeService
    {
        private readonly IGroupRepository _groupNestedSetRepository;
        private readonly DataBaseContext _context;

        public GroupTreeService(IGroupRepository groupNestedSetRepository, DataBaseContext context)
        {
            _groupNestedSetRepository = groupNestedSetRepository;
            _context = context;
        }

        /// <summary>
        /// Erstellt eine neue Gruppe als Kind einer bestehenden Gruppe
        /// </summary>
        public async Task<GroupTreeNodeResource> CreateGroupNode(Guid? parentId, GroupCreateResource groupResource, string currentUser)
        {
            var newGroup = new Group
            {
                Name = groupResource.Name,
                Description = groupResource.Description,
                ValidFrom = groupResource.ValidFrom,
                ValidUntil = groupResource.ValidUntil,
                CurrentUserCreated = currentUser,
                GroupItems = new List<GroupItem>()
            };

            Group createdGroup;

            if (parentId.HasValue)
            {
                createdGroup = await _groupNestedSetRepository.AddChildNode(parentId.Value, newGroup);
            }
            else
            {
                createdGroup = await _groupNestedSetRepository.AddRootNode(newGroup);
            }

            // Aktualisiere die Gruppenmitglieder, falls vorhanden
            if (groupResource.ClientIds != null && groupResource.ClientIds.Any())
            {
                foreach (var clientId in groupResource.ClientIds)
                {
                    var groupItem = new GroupItem
                    {
                        GroupId = createdGroup.Id,
                        ClientId = clientId
                    };
                    _context.GroupItem.Add(groupItem);
                }
                await _context.SaveChangesAsync();
            }

            return await GetGroupNodeDetails(createdGroup.Id);
        }

        /// <summary>
        /// Aktualisiert eine bestehende Gruppe
        /// </summary>
        public async Task<GroupTreeNodeResource> UpdateGroupNode(Guid id, GroupUpdateResource groupResource, string currentUser)
        {
            var group = await _context.Group
                .Include(g => g.GroupItems)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {id} nicht gefunden");
            }

            // Grundlegende Daten aktualisieren
            group.Name = groupResource.Name;
            group.Description = groupResource.Description;
            group.ValidFrom = groupResource.ValidFrom;
            group.ValidUntil = groupResource.ValidUntil;
            group.CurrentUserUpdated = currentUser;

            // Wenn ein neuer Elternteil angegeben wird und sich geändert hat
            if (groupResource.ParentId.HasValue && group.Parent != groupResource.ParentId)
            {
                await _groupNestedSetRepository.MoveNode(id, groupResource.ParentId.Value);
            }

            // Gruppenmitglieder aktualisieren
            if (groupResource.ClientIds != null)
            {
                var existingClientIds = group.GroupItems.Select(gi => gi.ClientId).ToList();
                var newClientIds = groupResource.ClientIds.ToList();

                // Zu entfernende Mitglieder
                var clientsToRemove = existingClientIds.Except(newClientIds).ToList();
                foreach (var clientId in clientsToRemove)
                {
                    var groupItem = group.GroupItems.FirstOrDefault(gi => gi.ClientId == clientId);
                    if (groupItem != null)
                    {
                        _context.GroupItem.Remove(groupItem);
                    }
                }

                // Neue Mitglieder hinzufügen
                var clientsToAdd = newClientIds.Except(existingClientIds).ToList();
                foreach (var clientId in clientsToAdd)
                {
                    var groupItem = new GroupItem
                    {
                        GroupId = id,
                        ClientId = clientId
                    };
                    _context.GroupItem.Add(groupItem);
                }
            }

            await _groupNestedSetRepository.UpdateNode(group);
            await _context.SaveChangesAsync();

            return await GetGroupNodeDetails(id);
        }

        /// <summary>
        /// Löscht eine Gruppe und all ihre Untergruppen
        /// </summary>
        public async Task DeleteGroupNode(Guid id, string currentUser)
        {
            var group = await _context.Group
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {id} nicht gefunden");
            }

            group.CurrentUserDeleted = currentUser;
            _context.Group.Update(group);
            await _context.SaveChangesAsync();

            await _groupNestedSetRepository.DeleteNode(id);
        }

        /// <summary>
        /// Holt Details zu einer spezifischen Gruppe, einschließlich Mitglieder und Metadaten
        /// </summary>
        public async Task<GroupTreeNodeResource> GetGroupNodeDetails(Guid id)
        {
            var group = await _context.Group
                .Include(g => g.GroupItems)
                .ThenInclude(gi => gi.Client)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {id} nicht gefunden");
            }

            var depth = await _groupNestedSetRepository.GetNodeDepth(id);
            var clientsResource = group.GroupItems.Select(gi => new ClientResource
            {
                Id = gi.ClientId,
                Name = gi.Client?.Name ?? string.Empty,
                // Weitere Eigenschaften hier hinzufügen
            }).ToList();

            return new GroupTreeNodeResource
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                ValidFrom = group.ValidFrom,
                ValidUntil = group.ValidUntil,
                ParentId = group.Parent,
                Root = group.Root,
                Lft = group.Lft,
                Rgt = group.rgt,
                Depth = depth,
                Clients = clientsResource,
                CreateTime = group.CreateTime,
                UpdateTime = group.UpdateTime,
                CurrentUserCreated = group.CurrentUserCreated,
                CurrentUserUpdated = group.CurrentUserUpdated
            };
        }

        /// <summary>
        /// Erstellt eine Baumdarstellung einer Gruppe und ihrer Untergruppen
        /// </summary>
        public async Task<GroupTreeResource> GetGroupTree(Guid? rootId = null)
        {
            var nodes = await _groupNestedSetRepository.GetTree(rootId);
            var nodesList = nodes.OrderBy(n => n.Lft).ToList();

            if (!nodesList.Any())
            {
                return new GroupTreeResource
                {
                    RootId = rootId,
                    Nodes = new List<GroupTreeNodeResource>()
                };
            }

            var result = new GroupTreeResource
            {
                RootId = rootId ?? nodesList.FirstOrDefault(n => n.Parent == null)?.Id,
                Nodes = new List<GroupTreeNodeResource>()
            };

            // Dictionary für schnellen Zugriff auf Tiefe basierend auf ID
            var depths = new Dictionary<Guid, int>();

            // Eine Hierarchie aus flachen Nodes bauen
            foreach (var node in nodesList)
            {
                // Tiefe berechnen
                var ancestorCount = nodesList.Count(n => n.Lft < node.Lft && n.rgt > node.rgt && n.Root == node.Root);
                depths[node.Id] = ancestorCount;

                var clientsCount = await _context.GroupItem.CountAsync(gi => gi.GroupId == node.Id);

                result.Nodes.Add(new GroupTreeNodeResource
                {
                    Id = node.Id,
                    Name = node.Name,
                    Description = node.Description,
                    ValidFrom = node.ValidFrom,
                    ValidUntil = node.ValidUntil,
                    ParentId = node.Parent,
                    Root = node.Root,
                    Lft = node.Lft,
                    Rgt = node.rgt,
                    Depth = ancestorCount,
                    ClientsCount = clientsCount,
                    CreateTime = node.CreateTime,
                    UpdateTime = node.UpdateTime
                });
            }

            return result;
        }

        /// <summary>
        /// Holt den Pfad von der Wurzel bis zum angegebenen Knoten
        /// </summary>
        public async Task<List<GroupTreeNodeResource>> GetPathToNode(Guid nodeId)
        {
            var pathNodes = await _groupNestedSetRepository.GetPath(nodeId);

            var result = new List<GroupTreeNodeResource>();
            var depth = 0;

            foreach (var node in pathNodes.OrderBy(n => n.Lft))
            {
                var clientsCount = await _context.GroupItem.CountAsync(gi => gi.GroupId == node.Id);

                result.Add(new GroupTreeNodeResource
                {
                    Id = node.Id,
                    Name = node.Name,
                    Description = node.Description,
                    ValidFrom = node.ValidFrom,
                    ValidUntil = node.ValidUntil,
                    ParentId = node.Parent,
                    Root = node.Root,
                    Lft = node.Lft,
                    Rgt = node.rgt,
                    Depth = depth++,
                    ClientsCount = clientsCount,
                    CreateTime = node.CreateTime,
                    UpdateTime = node.UpdateTime
                });
            }

            return result;
        }
    }
}