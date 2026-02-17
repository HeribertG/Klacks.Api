using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class LLMMapper
{
    [MapperIgnoreTarget(nameof(LLMProvider.Id))]
    [MapperIgnoreTarget(nameof(LLMProvider.CreateTime))]
    [MapperIgnoreTarget(nameof(LLMProvider.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(LLMProvider.UpdateTime))]
    [MapperIgnoreTarget(nameof(LLMProvider.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(LLMProvider.DeletedTime))]
    [MapperIgnoreTarget(nameof(LLMProvider.IsDeleted))]
    [MapperIgnoreTarget(nameof(LLMProvider.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(LLMProvider.Models))]
    [MapperIgnoreTarget(nameof(LLMProvider.ApiVersion))]
    [MapperIgnoreTarget(nameof(LLMProvider.Settings))]
    public partial LLMProvider ToProviderFromCreate(CreateProviderCommand command);

    [MapperIgnoreTarget(nameof(LLMProvider.Id))]
    [MapperIgnoreTarget(nameof(LLMProvider.ProviderId))]
    [MapperIgnoreTarget(nameof(LLMProvider.ProviderName))]
    [MapperIgnoreTarget(nameof(LLMProvider.CreateTime))]
    [MapperIgnoreTarget(nameof(LLMProvider.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(LLMProvider.UpdateTime))]
    [MapperIgnoreTarget(nameof(LLMProvider.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(LLMProvider.DeletedTime))]
    [MapperIgnoreTarget(nameof(LLMProvider.IsDeleted))]
    [MapperIgnoreTarget(nameof(LLMProvider.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(LLMProvider.Models))]
    [MapperIgnoreTarget(nameof(LLMProvider.ApiVersion))]
    [MapperIgnoreTarget(nameof(LLMProvider.Settings))]
    public partial LLMProvider ToProviderFromUpdate(UpdateProviderCommand command);

    public partial LLMProviderResource ToProviderResource(LLMProvider provider);

    public partial List<LLMProviderResource> ToProviderResources(List<LLMProvider> providers);

    public LLMUsageResponse ToUsageResponse(LLMUsageRawData rawData)
    {
        return new LLMUsageResponse
        {
            TotalTokens = rawData.Usages.Sum(u => u.TotalTokens),
            TotalCost = rawData.TotalCost,
            StartDate = rawData.StartDate,
            EndDate = rawData.EndDate,
            ModelUsage = rawData.ModelSummary,
            DailyUsage = rawData.Usages
                .GroupBy(u => u.CreateTime.HasValue ? u.CreateTime.Value.Date : DateTime.UtcNow.Date)
                .Select(g => new DailyUsage
                {
                    Date = g.Key,
                    Tokens = g.Sum(u => u.TotalTokens),
                    Cost = g.Sum(u => u.Cost),
                    Requests = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList()
        };
    }
}
