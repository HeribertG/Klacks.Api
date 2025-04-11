using Klacks.Api.Interfaces;
using Klacks.Api.Repositories;

namespace Klacks.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registriert alle für das Nested-Set-Modell notwendigen Services
    /// </summary>
    public static IServiceCollection AddGroupTreeServices(this IServiceCollection services)
    {
        // Repository registrieren
        services.AddScoped<IGroupNestedSetRepository, GroupNestedSetRepository>();

        return services;
    }
}