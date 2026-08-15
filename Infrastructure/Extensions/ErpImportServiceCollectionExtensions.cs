// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the ERP import feature's own authentication scheme and object storage client. Kept
/// separate from the main JWT AddAuthentication() call so the KlacksErpImport scheme never
/// becomes a default candidate for JWT-protected endpoints (the same scheme-pinning footgun
/// documented for SignalR hubs and the MCP endpoint: AddIdentity overrides the default scheme).
/// </summary>
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ErpImportServiceCollectionExtensions
{
    public static IServiceCollection AddErpObjectStorage(this IServiceCollection services)
    {
        services.AddScoped<IObjectStorageService, Services.Imports.FileSystemObjectStorageService>();

        return services;
    }

    public static IServiceCollection AddErpImportServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ErpObjectStorageOptions>(configuration.GetSection(ErpObjectStorageOptions.SectionName));

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ErpImportTokenAuthenticationHandler>(ErpImportTokenConstants.SchemeName, configureOptions: null);

        services.AddScoped<ErpCustomerResolver>();
        services.AddScoped<OrderSupersessionService>();
        services.AddScoped<IErpDefaultDropPointProvider, ErpDefaultDropPointProvider>();
        services.AddScoped<IErpImportExceptionRepository, Repositories.Imports.ErpImportExceptionRepository>();
        services.AddScoped<IErpOrderImportRunner, ErpOrderImportRunner>();

        var bgOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

        if (bgOptions.ErpOrderImport)
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Imports.ErpOrderImportBackgroundService>();

        return services;
    }
}
