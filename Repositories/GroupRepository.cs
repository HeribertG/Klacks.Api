using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Resources.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories
{
    public class GroupRepository : BaseRepository<Group>, IGroupRepository
    {
        private readonly DataBaseContext context;

        public GroupRepository(DataBaseContext context)
           : base(context)
        {
            this.context = context;
        }

        public new async Task Add(Group model)
        {
            if (model.Parent.HasValue)
            {
                await AddChildNode(model.Parent.Value, model);
            }
            else
            {
               await AddRootNode(model);
            }
        }

        public override async Task<Group?> Delete(Guid id)
        {
            var group = await context.Group.FirstOrDefaultAsync(g => g.Id == id);

            await DeleteNode(id);

            return group;
        }

        public async Task<Group> AddChildNode(Guid parentId, Group newGroup)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var parent = await context.Group
                    .Where(g => g.Id == parentId && !g.IsDeleted)
                    .FirstOrDefaultAsync();

                if (parent == null)
                {
                    throw new KeyNotFoundException($"Eltern-Gruppe mit ID {parentId} nicht gefunden");
                }

                // Platz schaffen für den neuen Knoten
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"rgt\" = \"rgt\" + 2 WHERE \"rgt\" > @p0 AND \"is_deleted\" = false",
                    parent.rgt);

                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"lft\" = \"lft\" + 2 WHERE \"lft\" > @p0 AND \"is_deleted\" = false",
                    parent.rgt);

                // Neuen Knoten einfügen
                newGroup.Lft = parent.rgt;
                newGroup.rgt = parent.rgt + 1;
                newGroup.Parent = parent.Id;
                newGroup.Root = parent.Root ?? parent.Id;
                newGroup.CreateTime = DateTime.UtcNow;

                context.Group.Add(newGroup);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
                return newGroup;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Group> AddRootNode(Group newGroup)
        {
            // Finden des höchsten rechten Wertes, um den neuen Baum anzufügen
            var maxRgt = await context.Group
                .Where(g => !g.IsDeleted && g.Root == null)
                .OrderByDescending(g => g.rgt)
                .Select(g => (int?)g.rgt)
                .FirstOrDefaultAsync() ?? 0;

            newGroup.Lft = maxRgt + 1;
            newGroup.rgt = maxRgt + 2;
            newGroup.Parent = null;
            newGroup.Root = null;
            newGroup.CreateTime = DateTime.UtcNow;

            context.Group.Add(newGroup);
            await context.SaveChangesAsync();

            // Aktualisiere die Root-ID für diesen neuen Wurzelknoten
            newGroup.Root = newGroup.Id;
            context.Group.Update(newGroup);
            await context.SaveChangesAsync();

            return newGroup;
        }

        public async Task DeleteNode(Guid id)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var node = await context.Group
                    .Where(g => g.Id == id && !g.IsDeleted)
                    .FirstOrDefaultAsync();

                if (node == null)
                {
                    throw new KeyNotFoundException($"Gruppe mit ID {id} nicht gefunden");
                }

                var width = node.rgt - node.Lft + 1;

                // Markiere diesen Knoten und alle Kindknoten als gelöscht
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"is_deleted\" = true, \"deleted_time\" = @p0 " +
                    "WHERE \"lft\" >= @p1 AND \"rgt\" <= @p2 AND \"root\" = @p3",
                    DateTime.UtcNow, node.Lft, node.rgt, node.Root);

                // Aktualisiere die Lft- und Rgt-Werte aller Knoten nach diesem
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"lft\" = \"lft\" - @p0 WHERE \"lft\" > @p1 AND \"root\" = @p2 AND \"is_deleted\" = false",
                    width, node.rgt, node.Root);

                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND \"root\" = @p2 AND \"is_deleted\" = false",
                    width, node.rgt, node.Root);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public IQueryable<Group> FilterGroup(GroupFilter filter)
        {
            var tmp = context.Group.Include(gr => gr.GroupItems)
                                   .ThenInclude(gi => gi.Client)
                                   .AsNoTracking()
                                   .OrderBy(g => g.Root)
                                   .AsQueryable();

            tmp = FilterByDateRange(filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange, tmp);
            tmp = FilterBySearchString(filter.SearchString, tmp);
            tmp = Sort(filter.OrderBy, filter.SortOrder, tmp);

            return tmp;
        }

        public new async Task<Group?> Get(Guid id)
        {
            return await context.Group.Where(x => x.Id == id).Include(x => x.GroupItems).ThenInclude(x => x.Client).AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Group>> GetChildren(Guid parentId)
        {
            var parent = await context.Group
                .Where(g => g.Id == parentId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                throw new KeyNotFoundException($"Eltern-Gruppe mit ID {parentId} nicht gefunden");
            }

            // Direkten Kinder sind ein Level tiefer
            return await context.Group
                .Where(g => g.Parent == parentId && !g.IsDeleted)
                .OrderBy(g => g.Lft)
                .ToListAsync();
        }

        public async Task<int> GetNodeDepth(Guid nodeId)
        {
            var node = await context.Group
                .Where(g => g.Id == nodeId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (node == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {nodeId} nicht gefunden");
            }

            // Die Tiefe ist die Anzahl der Vorfahren
            var depth = await context.Group
                .CountAsync(g => g.Lft < node.Lft && g.rgt > node.rgt && g.Root == node.Root && !g.IsDeleted);

            return depth;
        }

        public async Task<IEnumerable<Group>> GetPath(Guid nodeId)
        {
            var node = await context.Group
                .Where(g => g.Id == nodeId && !g.IsDeleted)
                .FirstOrDefaultAsync();

            if (node == null)
            {
                throw new KeyNotFoundException($"Gruppe mit ID {nodeId} nicht gefunden");
            }

            return await context.Group
                .Where(g => g.Lft <= node.Lft && g.rgt >= node.rgt && g.Root == node.Root)
                .OrderBy(g => g.Lft)
                .ToListAsync();
        }


        public async Task<IEnumerable<Group>> GetTree(Guid? rootId = null)
        {
            if (rootId.HasValue)
            {
                var root = await context.Group
                    .Where(g => g.Id == rootId)
                    .FirstOrDefaultAsync();

                if (root == null)
                {
                    throw new KeyNotFoundException($"Wurzel-Gruppe mit ID {rootId} nicht gefunden");
                }

                return await context.Group
                    .Where(g => g.Root == rootId )
                    .OrderBy(g => g.Root)
                    .ThenBy(g => g.Lft)
                    .ToListAsync();
            }
            else
            {
                // Alle Root-Nodes und ihre Kinder holen
                return await context.Group
                    .Where(g => !g.IsDeleted)
                    .OrderBy(g => g.Root)
                    .ThenBy(g => g.Lft)
                    .ToListAsync();
            }
        }

        public async Task MoveNode(Guid nodeId, Guid newParentId)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var node = await context.Group
                    .Where(g => g.Id == nodeId)
                    .FirstOrDefaultAsync();

                if (node == null)
                {
                    throw new KeyNotFoundException($"Zu verschiebende Gruppe mit ID {nodeId} nicht gefunden");
                }

                var newParent = await context.Group
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
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"lft\" = -\"lft\", \"rgt\" = -\"rgt\" " +
                    "WHERE \"lft\" >= @p0 AND \"rgt\" <= @p1 AND \"root\" = @p2",
                    node.Lft, node.rgt, node.Root);

                // Anpassen der Lücke, die durch das Verschieben des Knotens entsteht
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"lft\" = \"lft\" - @p0 WHERE \"lft\" > @p1 AND \"root\" = @p2",
                    nodeWidth, node.rgt, node.Root);

                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND \"root\" = @p2",
                    nodeWidth, node.rgt, node.Root);

                // Anpassen der Position, an der der Knoten eingefügt wird
                if (newPos > node.rgt)
                {
                    newPos -= nodeWidth;
                }

                // Platz für den zu verschiebenden Knoten schaffen
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"rgt\" = \"rgt\" + @p0 WHERE \"rgt\" >= @p1 AND \"root\" = @p2",
                    nodeWidth, newPos, newParent.Root);

                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"lft\" = \"lft\" + @p0 WHERE \"lft\" > @p1 AND \"root\" = @p2",
                    nodeWidth, newPos, newParent.Root);

                // Verschieben der Knoten an die neue Position und Aktualisieren des Root-Werts
                var offset = newPos - node.Lft;
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"group\" SET \"lft\" = -\"lft\" + @p0, \"rgt\" = -\"rgt\" + @p0, \"root\" = @p1 " +
                    "WHERE \"lft\" <= 0 AND \"root\" = @p2",
                    offset, newParent.Root, node.Root);

                // Aktualisiere den Parent-Wert des verschobenen Knotens
                node.Parent = newParentId;
                context.Group.Update(node);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public override Group Put(Group model)
        {
            // GroupItems aktualisieren
            var existingIds = context.GroupItem.Where(x => x.GroupId == model.Id).Select(x => x.ClientId).ToList();
            var modelListIds = model.GroupItems.Select(x => x.ClientId).ToList();

            var newIds = modelListIds.Where(x => !existingIds.Contains(x)).ToList();
            var deleteItems = existingIds.Where(x => !modelListIds.Contains(x)).ToList();

            if (newIds.Any())
            {
                var lst = CreateList(newIds, model.Id);
                context.GroupItem.AddRange(lst.ToArray());
            }

            foreach (var id in deleteItems)
            {
                var item = context.GroupItem.FirstOrDefault(x => x.ClientId == id);
                if (item != null)
                {
                    context.GroupItem.Remove(item);
                }
            }

            // Bestehende Gruppe abrufen
            var existingGroup = context.Group.AsNoTracking().FirstOrDefault(x => x.Id == model.Id);
            if (existingGroup != null)
            {
                // Prüfen, ob sich der Parent geändert hat
                if (existingGroup.Parent != model.Parent)
                {
                    // Grundlegende Daten aktualisieren
                    Task.Run(async () => await UpdateNode(model)).Wait();

                    // Wenn ein neuer Parent gesetzt ist, verschieben
                    if (model.Parent.HasValue)
                    {
                        Task.Run(async () => await MoveNode(model.Id, model.Parent.Value)).Wait();
                    }
                    // Wenn der Parent auf null gesetzt wurde, müsste hier eine Logik implementiert werden,
                    // um einen Knoten zu einem Root-Knoten zu machen
                }
                else
                {
                    // Nur grundlegende Daten aktualisieren
                    Task.Run(async () => await UpdateNode(model)).Wait();
                }
            }

            return model;
        }

        public async Task<TruncatedGroup> Truncated(GroupFilter filter)
        {
            var tmp = FilterGroup(filter);

            var count = tmp.Count();
            var maxPage = filter.NumberOfItemsPerPage > 0 ? (count / filter.NumberOfItemsPerPage) : 0;

            var firstItem = 0;

            if (count > 0 && count > filter.NumberOfItemsPerPage)
            {
                if ((filter.IsNextPage.HasValue || filter.IsPreviousPage.HasValue) && filter.FirstItemOnLastPage.HasValue)
                {
                    if (filter.IsNextPage.HasValue)
                    {
                        firstItem = filter.FirstItemOnLastPage.Value + filter.NumberOfItemsPerPage;
                    }
                    else
                    {
                        var numberOfItem = filter.NumberOfItemOnPreviousPage ?? filter.NumberOfItemsPerPage;
                        firstItem = filter.FirstItemOnLastPage.Value - numberOfItem;
                        if (firstItem < 0)
                        {
                            firstItem = 0;
                        }
                    }
                }
                else
                {
                    firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
                }
            }
            else
            {
                firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
            }

            tmp = tmp.Skip(firstItem).Take(filter.NumberOfItemsPerPage);

            var groups = count == 0 ? new List<Group>() : await tmp.ToListAsync();
            var res = new TruncatedGroup
            {
                Groups = groups,
                MaxItems = count,
            };

            if (filter.NumberOfItemsPerPage > 0)
            {
                res.MaxPages = count % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
            }

            res.CurrentPage = filter.RequiredPage;
            res.FirstItemOnPage = res.MaxItems <= firstItem ? -1 : firstItem;

            return res;
        }

        public async Task UpdateNode(Group updatedGroup)
        {
            var existingGroup = await context.Group
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

            context.Group.Update(existingGroup);
            await context.SaveChangesAsync();
        }


        private List<GroupItem> CreateList(List<Guid> list, Guid groubId)
        {
            var lst = new List<GroupItem>();
            foreach (var id in list)
            {
                lst.Add(new GroupItem() { ClientId = id, GroupId = groubId });
            }

            return lst;
        }

        private IQueryable<Group> FilterByDateRange(bool activeDateRange, bool formerDateRange, bool futureDateRange, IQueryable<Group> tmp)
        {
            if (activeDateRange && formerDateRange && futureDateRange)
            {
                // No need for filters
            }
            else if (!activeDateRange && !formerDateRange && !futureDateRange)
            {
                tmp = Enumerable.Empty<Group>().AsQueryable();
            }
            else
            {
                var nowDate = DateTime.Now;

                // only active
                if (activeDateRange && !formerDateRange && !futureDateRange)
                {
                    tmp = tmp.Where(co =>
                                    co.ValidFrom.Date <= nowDate &&
                                    (co.ValidUntil.HasValue == false ||
                                    (co.ValidUntil.HasValue && co.ValidUntil.Value.Date >= nowDate)
                                    ));
                }

                // only former
                if (!activeDateRange && formerDateRange && !futureDateRange)
                {
                    tmp = tmp.Where(co =>
                                   (co.ValidUntil.HasValue && co.ValidUntil.Value.Date < nowDate));
                }

                // only future
                if (!activeDateRange && !formerDateRange && futureDateRange)
                {
                    tmp = tmp.Where(co =>
                                   (co.ValidFrom.Date > nowDate));
                }

                // former + active
                if (activeDateRange && formerDateRange && !futureDateRange)
                {
                    tmp = tmp.Where(co =>
                                    co.ValidFrom.Date <= nowDate &&
                                    (co.ValidUntil.HasValue == false ||
                                    (co.ValidUntil.HasValue && co.ValidUntil.Value.Date > nowDate) ||
                                    (co.ValidUntil.HasValue && co.ValidUntil.Value.Date < nowDate)));
                }

                // active + future
                if (activeDateRange && !formerDateRange && futureDateRange)
                {
                    tmp = tmp.Where(co =>
                                     ((co.ValidFrom.Date <= nowDate &&
                                     (co.ValidUntil.HasValue == false ||
                                     (co.ValidUntil.HasValue && co.ValidUntil.Value.Date > nowDate))) ||
                                     (co.ValidFrom.Date > nowDate)
                                     ));
                }

                // former + future
                if (!activeDateRange && formerDateRange && futureDateRange)
                {
                    tmp = tmp.Where(co =>
                                  (co.ValidUntil.HasValue && co.ValidUntil.Value.Date < nowDate) ||
                                  (co.ValidFrom.Date > nowDate));
                }
            }

            return tmp;
        }

        private IQueryable<Group> FilterBySearchString(string searchString, IQueryable<Group> tmp)
        {
            var keywordList = searchString.TrimEnd().TrimStart().ToLower().Split(' ');

            if (keywordList.Length == 1)
            {
                if (keywordList[0].Length == 1)
                {
                    tmp = this.FilterBySearchStringFirstSymbol(keywordList[0], tmp);
                }
            }
            else
            {
                tmp = this.FilterBySearchStringStandart(keywordList, tmp);
            }

            return tmp;
        }

        private IQueryable<Group> FilterBySearchStringFirstSymbol(string keyword, IQueryable<Group> tmp)
        {
            tmp = tmp.Where(co =>
                co.Name.ToLower().Substring(0, 1) == keyword.ToLower());

            return tmp;
        }

        private IQueryable<Group> FilterBySearchStringStandart(string[] keywordList, IQueryable<Group> tmp)
        {
            var list = new List<Guid>();
            foreach (var keyword in keywordList)
            {
                foreach (var item in tmp)
                {
                    var tmpWord = keyword.TrimEnd().TrimStart().ToLower();

                    if (item.Name != null && item.Name.ToLower().Contains(tmpWord))
                    {
                        list.Add(item.Id);
                    }
                }
            }

            tmp = tmp.Where(x => list.Contains(x.Id));
            return tmp;
        }

        private IQueryable<Group> Sort(string orderBy, string sortOrder, IQueryable<Group> tmp)
        {
            if (sortOrder != string.Empty)
            {
                if (orderBy == "name")
                {
                    return sortOrder == "asc" ? tmp.OrderBy(x => x.Name) : tmp.OrderByDescending(x => x.Name);
                }
                else if (orderBy == "description")
                {
                    return sortOrder == "asc" ? tmp.OrderBy(x => x.Description) : tmp.OrderByDescending(x => x.Description);
                }
                else if (orderBy == "valid_from")
                {
                    return sortOrder == "asc" ? tmp.OrderBy(x => x.ValidFrom) : tmp.OrderByDescending(x => x.ValidFrom);
                }
                else if (orderBy == "valid_until")
                {
                    return sortOrder == "asc" ? tmp.OrderBy(x => x.ValidUntil) : tmp.OrderByDescending(x => x.ValidUntil);
                }
            }

            return tmp;
        }
    }
}
