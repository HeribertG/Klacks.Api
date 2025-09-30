using AutoMapper;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.AutoMapper;

public class BaseMappingProfile : Profile
{
    public BaseMappingProfile()
    {
    }
}

public static class MappingExtensions
{
    public static IMappingExpression<TSource, TDestination> IgnoreAuditFields<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> expression)
        where TDestination : BaseEntity
    {
        return expression
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore());
    }
}