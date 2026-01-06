using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Application.Mappers;

public static class MapperServiceCollectionExtensions
{
    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        services.AddSingleton<ClientMapper>();
        services.AddSingleton<ScheduleMapper>();
        services.AddSingleton<GroupMapper>();
        services.AddSingleton<SettingsMapper>();
        services.AddSingleton<AddressCommunicationMapper>();
        services.AddSingleton<AuthMapper>();
        services.AddSingleton<LLMMapper>();
        services.AddSingleton<FilterMapper>();
        services.AddSingleton<IdentityProviderMapper>();

        return services;
    }
}
