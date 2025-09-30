using AutoMapper;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Application.Queries.LLM;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.AutoMapper;

public class LLMMappingProfile : Profile
{
    public LLMMappingProfile()
    {
        CreateMap<CreateProviderCommand, LLMProvider>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Models, opt => opt.Ignore())
            .ForMember(dest => dest.ApiVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore());

        CreateMap<UpdateProviderCommand, LLMProvider>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProviderId, opt => opt.Ignore())
            .ForMember(dest => dest.ProviderName, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Models, opt => opt.Ignore())
            .ForMember(dest => dest.ApiVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore());

        CreateMap<LLMUsageRawData, LLMUsageResponse>()
            .ForMember(dest => dest.TotalTokens, opt => opt.MapFrom(src => src.Usages.Sum(u => u.TotalTokens)))
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
            .ForMember(dest => dest.ModelUsage, opt => opt.MapFrom(src => src.ModelSummary))
            .ForMember(dest => dest.DailyUsage, opt => opt.MapFrom(src => src.Usages
                .GroupBy(u => u.CreateTime.HasValue ? u.CreateTime.Value.Date : DateTime.UtcNow.Date)
                .Select(g => new DailyUsage
                {
                    Date = g.Key,
                    Tokens = g.Sum(u => u.TotalTokens),
                    Cost = g.Sum(u => u.Cost),
                    Requests = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList()));
    }
}