// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Staffs;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class GroupMapper
{
    [MapperIgnoreTarget(nameof(Group.CreateTime))]
    [MapperIgnoreTarget(nameof(Group.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Group.UpdateTime))]
    [MapperIgnoreTarget(nameof(Group.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Group.DeletedTime))]
    [MapperIgnoreTarget(nameof(Group.IsDeleted))]
    [MapperIgnoreTarget(nameof(Group.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Group.CalendarSelection))]
    [MapperIgnoreTarget(nameof(Group.Parent))]
    [MapperIgnoreTarget(nameof(Group.GroupItems))]
    [MapperIgnoreTarget(nameof(Group.Lft))]
    [MapperIgnoreTarget(nameof(Group.Rgt))]
    [MapperIgnoreTarget(nameof(Group.Root))]
    public partial Group ToGroupFromSimple(SimpleGroupResource resource);

    public partial SimpleGroupResource ToSimpleGroupResource(Group group);

    [MapperIgnoreTarget(nameof(GroupResource.Children))]
    [MapperIgnoreTarget(nameof(GroupResource.Depth))]
    [MapperIgnoreTarget(nameof(GroupResource.ShiftsCount))]
    [MapperIgnoreTarget(nameof(GroupResource.CustomersCount))]
    public partial GroupResource ToGroupResource(Group group);
    public partial List<GroupResource> ToGroupResources(List<Group> groups);

    [MapperIgnoreTarget(nameof(Group.CreateTime))]
    [MapperIgnoreTarget(nameof(Group.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Group.UpdateTime))]
    [MapperIgnoreTarget(nameof(Group.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Group.DeletedTime))]
    [MapperIgnoreTarget(nameof(Group.IsDeleted))]
    [MapperIgnoreTarget(nameof(Group.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Group.CalendarSelection))]
    [MapperIgnoreTarget(nameof(Group.Lft))]
    [MapperIgnoreTarget(nameof(Group.Rgt))]
    [MapperIgnoreTarget(nameof(Group.Root))]
    public partial Group ToGroupEntity(GroupResource resource);

    public GroupItemResource ToGroupItemResource(GroupItem item)
    {
        return new GroupItemResource
        {
            Id = item.Id,
            GroupId = item.GroupId,
            ClientId = item.ClientId,
            ShiftId = item.ShiftId,
            ValidFrom = item.ValidFrom,
            ValidUntil = item.ValidUntil,
            Client = item.Client != null ? new ClientResource
            {
                Id = item.Client.Id,
                IdNumber = item.Client.IdNumber,
                Company = item.Client.Company,
                FirstName = item.Client.FirstName,
                Name = item.Client.Name
            } : null
        };
    }

    [MapperIgnoreTarget(nameof(GroupItem.Group))]
    [MapperIgnoreTarget(nameof(GroupItem.Client))]
    [MapperIgnoreTarget(nameof(GroupItem.Shift))]
    [MapperIgnoreTarget(nameof(GroupItem.CreateTime))]
    [MapperIgnoreTarget(nameof(GroupItem.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(GroupItem.UpdateTime))]
    [MapperIgnoreTarget(nameof(GroupItem.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(GroupItem.DeletedTime))]
    [MapperIgnoreTarget(nameof(GroupItem.IsDeleted))]
    [MapperIgnoreTarget(nameof(GroupItem.CurrentUserDeleted))]
    public partial GroupItem ToGroupItemEntity(GroupItemResource resource);

    public partial TruncatedGroupResource ToTruncatedGroupResource(TruncatedGroup truncated);

    public partial GroupVisibilityResource ToGroupVisibilityResource(GroupVisibility visibility);
    public partial List<GroupVisibilityResource> ToGroupVisibilityResources(List<GroupVisibility> visibilities);

    [MapperIgnoreTarget(nameof(GroupVisibility.AppUser))]
    [MapperIgnoreTarget(nameof(GroupVisibility.Group))]
    [MapperIgnoreTarget(nameof(GroupVisibility.CreateTime))]
    [MapperIgnoreTarget(nameof(GroupVisibility.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(GroupVisibility.UpdateTime))]
    [MapperIgnoreTarget(nameof(GroupVisibility.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(GroupVisibility.DeletedTime))]
    [MapperIgnoreTarget(nameof(GroupVisibility.IsDeleted))]
    [MapperIgnoreTarget(nameof(GroupVisibility.CurrentUserDeleted))]
    public partial GroupVisibility ToGroupVisibilityEntity(GroupVisibilityResource resource);

    [MapProperty(nameof(AssignedGroup.Group) + "." + nameof(Group.Name), nameof(AssignedGroupResource.GroupName))]
    public partial AssignedGroupResource ToAssignedGroupResource(AssignedGroup assigned);

    [MapperIgnoreTarget(nameof(AssignedGroup.Client))]
    [MapperIgnoreTarget(nameof(AssignedGroup.Group))]
    [MapperIgnoreTarget(nameof(AssignedGroup.CreateTime))]
    [MapperIgnoreTarget(nameof(AssignedGroup.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(AssignedGroup.UpdateTime))]
    [MapperIgnoreTarget(nameof(AssignedGroup.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(AssignedGroup.DeletedTime))]
    [MapperIgnoreTarget(nameof(AssignedGroup.IsDeleted))]
    [MapperIgnoreTarget(nameof(AssignedGroup.CurrentUserDeleted))]
    public partial AssignedGroup ToAssignedGroupEntity(AssignedGroupResource resource);

    public GroupResource FromSummary(GroupSummary summary)
    {
        return new GroupResource
        {
            Id = Guid.NewGuid(),
            Name = summary.Name,
            Description = summary.Description,
            ValidFrom = summary.ValidFrom.HasValue
                ? summary.ValidFrom.Value.ToDateTime(TimeOnly.MinValue)
                : DateTime.MinValue,
            ValidUntil = summary.ValidTo.HasValue
                ? summary.ValidTo.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            Parent = summary.ParentId.HasValue ? Guid.NewGuid() : null,
            Lft = summary.LeftValue,
            Rgt = summary.RightValue
        };
    }

    public Group SummaryToEntity(GroupSummary summary)
    {
        return new Group
        {
            Id = Guid.NewGuid(),
            Name = summary.Name,
            Description = summary.Description,
            ValidFrom = summary.ValidFrom.HasValue
                ? summary.ValidFrom.Value.ToDateTime(TimeOnly.MinValue)
                : DateTime.MinValue,
            ValidUntil = summary.ValidTo.HasValue
                ? summary.ValidTo.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            Lft = summary.LeftValue,
            Rgt = summary.RightValue,
            IsDeleted = !summary.IsActive,
            CreateTime = summary.CreateTime,
            UpdateTime = summary.UpdateTime
        };
    }

    public BaseTruncatedResult ToBaseTruncatedResult(PagedResult<GroupSummary> pagedResult)
    {
        return new BaseTruncatedResult
        {
            MaxItems = pagedResult.TotalCount,
            MaxPages = pagedResult.TotalPages,
            CurrentPage = pagedResult.PageNumber,
            FirstItemOnPage = (pagedResult.PageNumber - 1) * pagedResult.PageSize + 1
        };
    }

    public TruncatedGroup ToTruncatedGroup(PagedResult<GroupSummary> pagedResult)
    {
        return new TruncatedGroup
        {
            Groups = pagedResult.Items.Select(SummaryToEntity).ToList(),
            MaxItems = pagedResult.TotalCount,
            MaxPages = pagedResult.TotalPages,
            CurrentPage = pagedResult.PageNumber,
            FirstItemOnPage = (pagedResult.PageNumber - 1) * pagedResult.PageSize + 1
        };
    }

    public GroupSearchCriteria ToGroupSearchCriteria(GroupFilter filter)
    {
        return new GroupSearchCriteria
        {
            FirstItemOnLastPage = filter.FirstItemOnLastPage,
            IsNextPage = filter.IsNextPage,
            IsPreviousPage = filter.IsPreviousPage,
            NumberOfItemsPerPage = filter.NumberOfItemsPerPage,
            OrderBy = filter.OrderBy,
            RequiredPage = filter.RequiredPage,
            SortOrder = filter.SortOrder,
            SearchString = filter.SearchString,
            ActiveDateRange = filter.ActiveDateRange,
            FormerDateRange = filter.FormerDateRange,
            FutureDateRange = filter.FutureDateRange
        };
    }
}
