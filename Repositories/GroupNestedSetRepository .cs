using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories
{
    /// <summary>
    /// Repository für die Verwaltung des Nested-Set-Modells der Gruppenstruktur
    /// Arbeitet mit dem bestehenden GroupRepository zusammen
    /// </summary>
    public class GroupNestedSetRepository : IGroupNestedSetRepository
    {
        private readonly DataBaseContext _context;

        public GroupNestedSetRepository(DataBaseContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Fügt eine neue Gruppe als Kind einer bestehenden Gruppe hinzu
        /// </summary>
        public async Task<Group> AddChildNode(Guid parentId, Group newGroup)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var parent = await _context.Group
                    .Where(g => g.Id == parentId && !g.IsDeleted)
                    .FirstOrDefaultAsync();

                if (parent == null)
                {
                    throw new KeyNotFoundException($"Eltern-Gruppe mit ID {parentId} nicht gefunden");
                }

                // Platz schaffen für den neuen Knoten
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"rgt\" = \"rgt\" + 2 WHERE \"rgt\" > @p0 AND \"IsDeleted\" = false",
                    parent.rgt);

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"Lft\" = \"Lft\" + 2 WHERE \"Lft\" > @p0 AND \"IsDeleted\" = false",
                    parent.rgt);

                // Neuen Knoten einfügen
                newGroup.Lft = parent.rgt;
                newGroup.rgt = parent.rgt + 1;
                newGroup.Parent = parent.Id;
                newGroup.Root = parent.Root ?? parent.Id;
                newGroup.CreateTime = DateTime.UtcNow;

                _context.Group.Add(newGroup);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return newGroup;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Fügt eine neue Wurzelgruppe hinzu
        /// </summary>
        public async Task<Group> AddRootNode(Group newGroup)
        {
            // Finden des höchsten rechten Wertes, um den neuen Baum anzufügen
            var maxRgt = await _context.Group
                .Where(g => !g.IsDeleted && g.Root == null)
                .OrderByDescending(g => g.rgt)
                .Select(g => (int?)g.rgt)
                .FirstOrDefaultAsync() ?? 0;

            newGroup.Lft = maxRgt + 1;
            newGroup.rgt = maxRgt + 2;
            newGroup.Parent = null;
            newGroup.Root = null;
            newGroup.CreateTime = DateTime.UtcNow;

            _context.Group.Add(newGroup);
            await _context.SaveChangesAsync();

            // Aktualisiere die Root-ID für diesen neuen Wurzelknoten
            newGroup.Root = newGroup.Id;
            _context.Group.Update(newGroup);
            await _context.SaveChangesAsync();

            return newGroup;
        }

        /// <summary>
        /// Löscht eine Gruppe und all ihre Untergruppen
        /// </summary>
        public async Task DeleteNode(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var node = await _context.Group
                    .Where(g => g.Id == id && !g.IsDeleted)
                    .FirstOrDefaultAsync();

                if (node == null)
                {
                    throw new KeyNotFoundException($"Gruppe mit ID {id} nicht gefunden");
                }

                var width = node.rgt - node.Lft + 1;

                // Markiere diesen Knoten und alle Kindknoten als gelöscht
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"IsDeleted\" = true, \"DeletedTime\" = @p0 " +
                    "WHERE \"Lft\" >= @p1 AND \"rgt\" <= @p2 AND \"Root\" = @p3",
                    DateTime.UtcNow, node.Lft, node.rgt, node.Root);

                // Aktualisiere die Lft- und Rgt-Werte aller Knoten nach diesem
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"Lft\" = \"Lft\" - @p0 WHERE \"Lft\" > @p1 AND \"Root\" = @p2 AND \"IsDeleted\" = false",
                    width, node.rgt, node.Root);

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND \"Root\" = @p2 AND \"IsDeleted\" = false",
                    width, node.rgt, node.Root);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Verschiebt einen Knoten zu einem neuen Elternteil
        /// </summary>
        public async Task MoveNode(Guid nodeId, Guid newParentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var node = await _context.Group
                    .Where(g => g.Id == nodeId && !g.IsDeleted)
                    .FirstOrDefaultAsync();

                if (node == null)
                {
                    throw new KeyNotFoundException($"Zu verschiebende Gruppe mit ID {nodeId} nicht gefunden");
                }

                var newParent = await _context.Group
                    .Where(g => g.Id == newParentId && !g.IsDeleted)
                    .FirstOrDefaultAsync();

                if (newParent == null)
                {
                    throw new KeyNotFoundException($"Neue Eltern-Gruppe mit ID {newParentId} nicht gefunden");
                }

                // Prüfen, ob der neue Elternteil nicht ein Nachkomme des zu verschiebenden Knotens ist
                if (newParent.Lft > node.Lft && newParent.rgt < node.rgt)
                {
                    throw new InvalidOperationException("Der neue Elternteil kann nicht ein Nachkomme des zu verschiebenden Knotens sein");
                }

                var nodeWidth = node.rgt - node.Lft + 1;
                var newPos = newParent.rgt;

                // Temporäre Markierung der zu verschiebenden Knoten mit negativen Werten
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"Lft\" = -\"Lft\", \"rgt\" = -\"rgt\" " +
                    "WHERE \"Lft\" >= @p0 AND \"rgt\" <= @p1 AND \"Root\" = @p2",
                    node.Lft, node.rgt, node.Root);

                // Anpassen der Lücke, die durch das Verschieben des Knotens entsteht
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"Lft\" = \"Lft\" - @p0 WHERE \"Lft\" > @p1 AND \"Root\" = @p2",
                    nodeWidth, node.rgt, node.Root);

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND \"Root\" = @p2",
                    nodeWidth, node.rgt, node.Root);

                // Anpassen der Position, an der der Knoten eingefügt wird
                if (newPos > node.rgt)
                {
                    newPos -= nodeWidth;
                }

                // Platz für den zu verschiebenden Knoten schaffen
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"rgt\" = \"rgt\" + @p0 WHERE \"rgt\" >= @p1 AND \"Root\" = @p2",
                    nodeWidth, newPos, newParent.Root);

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"Lft\" = \"Lft\" + @p0 WHERE \"Lft\" > @p1 AND \"Root\" = @p2",
                    nodeWidth, newPos, newParent.Root);

                // Verschieben der Knoten an die neue Position und Aktualisieren des Root-Werts
                var offset = newPos - node.Lft;
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Group\" SET \"Lft\" = -\"Lft\" + @p0, \"rgt\" = -\"rgt\" + @p0, \"Root\" = @p1 " +
                    "WHERE \"Lft\" <= 0 AND \"Root\" = @p2",
                    offset, newParent.Root, node.Root);

                // Aktualisiere den Parent-Wert des verschobenen Knotens
                node.Parent = newParentId;
                _context.Group.Update(node);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Holt alle direkten Untergruppen einer Gruppe
        /// </summary>
        public async Task<IEnumerable<Group>> GetChildren(Guid parentId)
        {
            var parent = await _context.Group
                .Where(g => g.Id == parentId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                throw new KeyNotFoundException($"Eltern-Gruppe mit ID {parentId} nicht gefunden");
            }

            // Direkten Kinder sind ein Level tiefer
            return await _context.Group
                .Where(g => g.Parent == parentId && !g.IsDeleted)
                .OrderBy(g => g.Lft)
                .ToListAsync();
        }

        /// <summary>
        /// Holt den kompletten Baum von einer bestimmten Wurzel aus
        /// </summary>
        public async Task<IEnumerable<Group>> GetTree(Guid? rootId = null)
        {
            if (rootId.HasValue)
            {
                var root = await _context.Group
                    .Where(g => g.Id == rootId && !g.IsDeleted)
                    .FirstOrDefaultAsync();

                if (root == null)
                {
                    throw new KeyNotFoundException($"Wurzel-Gruppe mit ID {rootId} nicht gefunden");
                }

                return await _context.Group
                    .Where(g => g.Root == rootId && !g.IsDeleted)
                    .OrderBy(g => g.Lft)
                    .ToListAsync();
            }
            else
            {
                // Alle Root-Nodes und ihre Kinder holen
                return await _context.Group
                    .Where(g => !g.IsDeleted)
                    .OrderBy(g => g.Root)
                    .ThenBy(g => g.Lft)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Holt den Pfad von der Wurzel bis zum angegebenen Knoten
        /// </summary>
        public async Task<IEnumerable<Group>> GetPath(Guid nodeId)
        {
            var node = await _context.Group
                .Where(g => g.Id == nodeId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (node == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {nodeId} nicht gefunden");
            }

            return await _context.Group
                .Where(g => g.Lft <= node.Lft && g.rgt >= node.rgt && g.Root == node.Root && !g.IsDeleted)
                .OrderBy(g => g.Lft)
                .ToListAsync();
        }

        /// <summary>
        /// Aktualisiert die Informationen einer Gruppe (ohne die Baumstruktur zu ändern)
        /// </summary>
        public async Task UpdateNode(Group updatedGroup)
        {
            var existingGroup = await _context.Group
                .Where(g => g.Id == updatedGroup.Id && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (existingGroup == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {updatedGroup.Id} nicht gefunden");
            }

            // Nur die grundlegenden Informationen aktualisieren, nicht die Baumstruktur
            existingGroup.Name = updatedGroup.Name;
            existingGroup.Description = updatedGroup.Description;
            existingGroup.ValidFrom = updatedGroup.ValidFrom;
            existingGroup.ValidUntil = updatedGroup.ValidUntil;
            existingGroup.UpdateTime = DateTime.UtcNow;
            existingGroup.CurrentUserUpdated = updatedGroup.CurrentUserUpdated;

            _context.Group.Update(existingGroup);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Berechnet die Tiefe eines Knotens im Baum
        /// </summary>
        public async Task<int> GetNodeDepth(Guid nodeId)
        {
            var node = await _context.Group
                .Where(g => g.Id == nodeId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (node == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {nodeId} nicht gefunden");
            }

            // Die Tiefe ist die Anzahl der Vorfahren
            var depth = await _context.Group
                .CountAsync(g => g.Lft < node.Lft && g.rgt > node.rgt && g.Root == node.Root && !g.IsDeleted);

            return depth;
        }
    }
}