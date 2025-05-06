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
            var groupEntity = await context.Group
                .Where(g => g.Id == id)
                .FirstOrDefaultAsync();

            if (groupEntity == null)
            {
                throw new KeyNotFoundException($"Group with ID {id} not found");
            }

            var width = groupEntity.Rgt - groupEntity.Lft + 1;


            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"is_deleted\" = true, \"deleted_time\" = @p0 " +
                "WHERE \"lft\" >= @p1 AND \"rgt\" <= @p2 AND \"root\" = @p3",
                DateTime.UtcNow, groupEntity.Lft, groupEntity.Rgt, groupEntity.Root);


            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"lft\" = \"lft\" - @p0 WHERE \"lft\" > @p1 AND \"root\" = @p2 AND \"is_deleted\" = false",
                width, groupEntity.Rgt, groupEntity.Root);

            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND \"root\" = @p2 AND \"is_deleted\" = false",
                width, groupEntity.Rgt, groupEntity.Root);

            return groupEntity;
        }

        private async Task<Group> AddChildNode(Guid parentId, Group newGroup)
        {
            var parent = await context.Group
                .Where(g => g.Id == parentId )
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                throw new KeyNotFoundException($"Parent group with ID {parentId} not found");
            }


            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"rgt\" = \"rgt\" + 2 WHERE \"rgt\" >= @p0 AND \"root\" = @p1 AND \"is_deleted\" = false",
                parent.Rgt, parent.Root);

            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"lft\" = \"lft\" + 2 WHERE \"lft\" > @p0 AND \"root\" = @p1 AND \"is_deleted\" = false",
                parent.Rgt, parent.Root);


            newGroup.Lft = parent.Rgt;
            newGroup.Rgt = parent.Rgt + 1;
            newGroup.Parent = parent.Id;
            newGroup.Root = parent.Root ?? parent.Id;
            newGroup.CreateTime = DateTime.UtcNow;

            context.Group.Add(newGroup);

            return newGroup;
        }

        public async Task<Group> AddRootNode(Group newGroup)
        {

            var maxRgt = await context.Group
                .Where(g =>  g.Root == null)
                .OrderByDescending(g => g.Rgt)
                .Select(g => (int?)g.Rgt)
                .FirstOrDefaultAsync() ?? 0;

            newGroup.Lft = maxRgt + 1;
            newGroup.Rgt = maxRgt + 2;
            newGroup.Parent = null;
            newGroup.Root = null;
            newGroup.CreateTime = DateTime.UtcNow;

            context.Group.Add(newGroup);

            return newGroup;
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
                .Where(g => g.Id == parentId  )
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                throw new KeyNotFoundException($"Parent group with ID {parentId} not found");
            }


            return await context.Group
                .Where(g => g.Parent == parentId  )
                .OrderBy(g => g.Lft)
                .ToListAsync();
        }

        public async Task<int> GetNodeDepth(Guid nodeId)
        {
            var node = await context.Group
                .Where(g => g.Id == nodeId  )
                .FirstOrDefaultAsync();

            if (node == null)
            {
                throw new KeyNotFoundException($"Group with ID {nodeId} not found");
            }


            var depth = await context.Group
                .CountAsync(g => g.Lft < node.Lft && g.Rgt > node.Rgt && g.Root == node.Root  );

            return depth;
        }

        public async Task<IEnumerable<Group>> GetPath(Guid nodeId)
        {
            var node = await context.Group
                .Where(g => g.Id == nodeId  )
                .FirstOrDefaultAsync();

            if (node == null)
            {
                throw new KeyNotFoundException($"Group with ID  {nodeId}  not found");
            }

            return await context.Group
                .Where(g => g.Lft <= node.Lft && g.Rgt >= node.Rgt && g.Root == node.Root)
                .OrderBy(g => g.Lft)
                .ToListAsync();
        }

        public async Task<IEnumerable<Group>> GetTree(Guid? rootId = null)
        {
            if (rootId.HasValue)
            {
                var root = await context.Group
                    .Where(g => g.Id == rootId)
                    .Include(g => g.GroupItems)
                    .ThenInclude(gi => gi.Client)
                    .FirstOrDefaultAsync();

                if (root == null)
                {
                    throw new KeyNotFoundException($"Root group with ID {rootId} not found");
                }

                return await context.Group
                    .Where(g => g.Root == rootId)
                    .Include(g => g.GroupItems)
                    .ThenInclude(gi => gi.Client)
                    .OrderBy(g => g.Name) // Primäre Sortierung nach Namen
                    .ThenBy(g => g.Root)  // Sekundäre Sortierung
                    .ThenBy(g => g.Lft)   // Tertiäre Sortierung für die Hierarchie
                    .ToListAsync();
            }
            else
            {
                return await context.Group
                    .Include(g => g.GroupItems)
                    .ThenInclude(gi => gi.Client)
                    .OrderBy(g => g.Name) // Primäre Sortierung nach Namen
                    .ThenBy(g => g.Root)  // Sekundäre Sortierung
                    .ThenBy(g => g.Lft)   // Tertiäre Sortierung für die Hierarchie
                    .ToListAsync();
            }
        }


        public async Task MoveNode(Guid nodeId, Guid newParentId)
        {
            var node = await context.Group
                .Where(g => g.Id == nodeId)
                .FirstOrDefaultAsync();

            if (node == null)
            {
                throw new KeyNotFoundException($"Group to be moved with ID {nodeId} not found");
            }

            var newParent = await context.Group
                .Where(g => g.Id == newParentId  )
                .FirstOrDefaultAsync();

            if (newParent == null)
            {
                throw new KeyNotFoundException($"New parent group with ID {newParentId} not found");
            }


            if (newParent.Lft > node.Lft && newParent.Rgt < node.Rgt)
            {
                throw new InvalidOperationException("The new parent cannot be a descendant of the groupEntity to be moved");
            }

            var nodeWidth = node.Rgt - node.Lft + 1;
            var newPos = newParent.Rgt;


            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"lft\" = -\"lft\", \"rgt\" = -\"rgt\" " +
                "WHERE \"lft\" >= @p0 AND \"rgt\" <= @p1 AND \"root\" = @p2",
                node.Lft, node.Rgt, node.Root);


            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"lft\" = \"lft\" - @p0 WHERE \"lft\" > @p1 AND \"root\" = @p2",
                nodeWidth, node.Rgt, node.Root);

            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND \"root\" = @p2",
                nodeWidth, node.Rgt, node.Root);


            if (newPos > node.Rgt)
            {
                newPos -= nodeWidth;
            }


            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"rgt\" = \"rgt\" + @p0 WHERE \"rgt\" >= @p1 AND \"root\" = @p2",
                nodeWidth, newPos, newParent.Root);

            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"lft\" = \"lft\" + @p0 WHERE \"lft\" > @p1 AND \"root\" = @p2",
                nodeWidth, newPos, newParent.Root);


            var offset = newPos - node.Lft;
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"group\" SET \"lft\" = -\"lft\" + @p0, \"rgt\" = -\"rgt\" + @p0, \"root\" = @p1 " +
                "WHERE \"lft\" <= 0 AND \"root\" = @p2",
                offset, newParent.Root, node.Root);


            node.Parent = newParentId;
            context.Group.Update(node);
        }


        public override async Task<Group?> Put(Group model)
        {
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

            var existingGroup = await context.Group.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existingGroup != null)
            {
                if (existingGroup.Parent != model.Parent)
                {
                    UpdateNode(model, existingGroup);

                    if (model.Parent.HasValue)
                    {
                        await MoveNode(model.Id, model.Parent.Value);
                    }
                }
                else
                {
                    UpdateNode(model, existingGroup);
                }
            }
            else
            {
                throw new KeyNotFoundException($"Group with ID {model.Id} not found");
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

        private  void UpdateNode(Group updatedGroup,Group existingGroup)
        {
           
            existingGroup.Name = updatedGroup.Name;
            existingGroup.Description = updatedGroup.Description;
            existingGroup.ValidFrom = updatedGroup.ValidFrom;
            existingGroup.ValidUntil = updatedGroup.ValidUntil;
           
            context.Group.Update(existingGroup);
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
                tmp = this.FilterBySearchStringStandard(keywordList, tmp);
            }

            return tmp;
        }

        private IQueryable<Group> FilterBySearchStringFirstSymbol(string keyword, IQueryable<Group> tmp)
        {
            tmp = tmp.Where(co =>
                co.Name.ToLower().Substring(0, 1) == keyword.ToLower());

            return tmp;
        }

        private IQueryable<Group> FilterBySearchStringStandard(string[] keywordList, IQueryable<Group> tmp)
        {
            foreach (var keyword in keywordList)
            {
                var trimmedKeyword = keyword.Trim().ToLower();
                tmp = tmp.Where(g => g.Name.ToLower().Contains(trimmedKeyword));
            }
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