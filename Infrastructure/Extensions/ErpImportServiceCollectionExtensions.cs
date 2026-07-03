// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the ERP import feature's own authentication scheme and object storage client. Kept
/// separate from the main JWT AddAuthentication() call so the KlacksErpImport scheme never
/// becomes a default candidate for JWT-protected endpoints (the same scheme-pinning footgun
/// documented for SignalR hubs and the MCP endpoint: AddIdentity overrides the default scheme).
/// </summary>
using Amazon.S3;
using Amazon.Runtime;
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
        services.AddScoped<Services.Imports.S3ObjectStorageService>();
        services.AddScoped<Services.Imports.FileSystemObjectStorageService>();
        services.AddScoped<IObjectStorageService>(provider =>
        {
            var storageOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ErpObjectStorageOptions>>().Value;
            return storageOptions.Provider == Domain.Enums.ErpObjectStorageProvider.S3
                ? provider.GetRequiredService<Services.Imports.S3ObjectStorageService>()
                : provider.GetRequiredService<Services.Imports.FileSystemObjectStorageService>();
        });

        return services;
    }

    public static IServiceCollection AddErpImportServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ErpObjectStorageOptions>(configuration.GetSection(ErpObjectStorageOptions.SectionName));

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ErpObjectStorageOptions>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = options.Region
            };

            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ErpImportTokenAuthenticationHandler>(ErpImportTokenConstants.SchemeName, configureOptions: null);

        services.AddScoped<ErpCustomerResolver>();
        services.AddScoped<OrderSupersessionService>();
        services.AddScoped<IErpDefaultDropPointProvider, ErpDefaultDropPointProvider>();
        services.AddScoped<IErpImportExceptionRepository, Repositories.Imports.ErpImportExceptionRepository>();
        services.AddScoped<IErpOrderImportRunner, ErpOrderImportRunner>();
        services.AddHostedService<Klacks.Api.Infrastructure.Services.Imports.ErpOrderImportBackgroundService>();

        return services;
    }
}
