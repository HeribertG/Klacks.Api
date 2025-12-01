using AutoMapper;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.AutoMapper;

public class GroupMappingProfile : Profile
{
    public GroupMappingProfile()
    {
        CreateMap<SimpleGroupResource, Group>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.Ignore())
            .ForMember(dest => dest.Rgt, opt => opt.Ignore())
            .ForMember(dest => dest.Root, opt => opt.Ignore());

        CreateMap<Group, SimpleGroupResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidUntil));

        CreateMap<Group, GroupResource>()
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.Depth, opt => opt.Ignore())
            .ForMember(dest => dest.ShiftsCount, opt => opt.Ignore())
            .ForMember(dest => dest.CustomersCount, opt => opt.Ignore())
            .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src => src.GroupItems));

        CreateMap<GroupResource, Group>()
            .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src => src.GroupItems))
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.Ignore())
            .ForMember(dest => dest.Rgt, opt => opt.Ignore())
            .ForMember(dest => dest.Root, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore());

        CreateMap<GroupItem, GroupItemResource>()
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client != null ? src.Client : null));

        CreateMap<GroupItemResource, GroupItem>()
            .ForMember(dest => dest.Group, opt => opt.Ignore())
            .ForMember(dest => dest.Client, opt => opt.Ignore())
            .ForMember(dest => dest.Shift, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore());

        CreateMap<TruncatedGroup, TruncatedGroupResource>();

        CreateMap<GroupVisibility, GroupVisibilityResource>();

        CreateMap<GroupVisibilityResource, GroupVisibility>()
            .ForMember(dest => dest.AppUser, opt => opt.Ignore())
            .ForMember(dest => dest.Group, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore());

        CreateMap<AssignedGroup, AssignedGroupResource>()
            .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Group.Name));

        CreateMap<AssignedGroupResource, AssignedGroup>()
            .ForMember(dest => dest.Client, opt => opt.Ignore())
            .ForMember(dest => dest.Group, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore());

        CreateMap<GroupSummary, GroupResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom.HasValue ? src.ValidFrom.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidTo.HasValue ? src.ValidTo.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.ParentId.HasValue ? Guid.NewGuid() : (Guid?)null))
            .ForMember(dest => dest.Root, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.MapFrom(src => src.LeftValue))
            .ForMember(dest => dest.Rgt, opt => opt.MapFrom(src => src.RightValue))
            .ForMember(dest => dest.Depth, opt => opt.Ignore())
            .ForMember(dest => dest.ShiftsCount, opt => opt.Ignore())
            .ForMember(dest => dest.CustomersCount, opt => opt.Ignore());

        CreateMap<GroupSummary, Group>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom.HasValue ? src.ValidFrom.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidTo.HasValue ? src.ValidTo.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Root, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.MapFrom(src => src.LeftValue))
            .ForMember(dest => dest.Rgt, opt => opt.MapFrom(src => src.RightValue))
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => !src.IsActive))
            .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
            .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdateTime));

        CreateMap<PagedResult<GroupSummary>, BaseTruncatedResult>()
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => (src.PageNumber - 1) * src.PageSize + 1));

        CreateMap<PagedResult<GroupSummary>, TruncatedGroup>()
            .ForMember(dest => dest.Groups, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => (src.PageNumber - 1) * src.PageSize + 1));

        CreateMap<Presentation.DTOs.Filter.GroupFilter, GroupSearchCriteria>()
            .ForMember(dest => dest.FirstItemOnLastPage, opt => opt.MapFrom(src => src.FirstItemOnLastPage))
            .ForMember(dest => dest.IsNextPage, opt => opt.MapFrom(src => src.IsNextPage))
            .ForMember(dest => dest.IsPreviousPage, opt => opt.MapFrom(src => src.IsPreviousPage))
            .ForMember(dest => dest.NumberOfItemsPerPage, opt => opt.MapFrom(src => src.NumberOfItemsPerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.RequiredPage, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder))
            .ForMember(dest => dest.SearchString, opt => opt.MapFrom(src => src.SearchString))
            .ForMember(dest => dest.ActiveDateRange, opt => opt.MapFrom(src => src.ActiveDateRange))
            .ForMember(dest => dest.FormerDateRange, opt => opt.MapFrom(src => src.FormerDateRange))
            .ForMember(dest => dest.FutureDateRange, opt => opt.MapFrom(src => src.FutureDateRange))
            .ForMember(dest => dest.ValidFromDate, opt => opt.Ignore())
            .ForMember(dest => dest.ValidToDate, opt => opt.Ignore())
            .ForMember(dest => dest.ParentGroupId, opt => opt.Ignore())
            .ForMember(dest => dest.IncludeSubGroups, opt => opt.Ignore())
            .ForMember(dest => dest.MaxDepth, opt => opt.Ignore())
            .ForMember(dest => dest.HasMembers, opt => opt.Ignore())
            .ForMember(dest => dest.MinMemberCount, opt => opt.Ignore())
            .ForMember(dest => dest.MaxMemberCount, opt => opt.Ignore());
    }
}