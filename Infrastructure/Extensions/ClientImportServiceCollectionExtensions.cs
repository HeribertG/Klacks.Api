// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the employee import: file readers, the synonym catalog (loaded once from the embedded
/// resource), detection/evaluation services, repositories, the template writer and the background
/// address geocoding. With BackgroundServices:AddressGeocoding off, or with geocoding switched off by the GeoServices
/// configuration (same evaluation as IGeoServicesPolicy), a disabled queue is registered so the import never blocks
/// on a queue nobody drains.
/// </summary>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Application.Services.Geocoding;
using Klacks.Api.Domain.Models.Settings.GeoServices;
using Klacks.Api.Domain.Services.GeoServices;
using Klacks.Api.Infrastructure.Repositories.Imports;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Services.ClientImport;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ClientImportServiceCollectionExtensions
{
    public static IServiceCollection AddClientImportServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => ClientImportSynonymCatalog.LoadEmbedded());
        services.AddSingleton<IClientImportFileReader, XlsxClientImportFileReader>();
        services.AddSingleton<IClientImportFileReader, CsvClientImportFileReader>();
        services.AddSingleton<IClientImportTemplateBuilder, ClientImportTemplateBuilder>();
        services.AddSingleton<ClientImportColumnDetector>();
        services.AddSingleton<ClientImportTransformer>();
        services.AddScoped<ClientImportEvaluator>();
        services.AddScoped<IClientImportBatchRepository, ClientImportBatchRepository>();
        services.AddScoped<IClientImportLookupRepository, ClientImportLookupRepository>();
        services.AddScoped<IAddressGeocodingProcessor, AddressGeocodingProcessor>();

        var backgroundOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

        if (backgroundOptions.AddressGeocoding && IsGeocodingEnabled(configuration))
        {
            services.AddSingleton<AddressGeocodingBackgroundService>();
            services.AddSingleton<IAddressGeocodingQueue>(sp => sp.GetRequiredService<AddressGeocodingBackgroundService>());
            services.AddHostedService(sp => sp.GetRequiredService<AddressGeocodingBackgroundService>());
        }
        else
        {
            services.AddSingleton<IAddressGeocodingQueue, DisabledAddressGeocodingQueue>();
        }

        return services;
    }

    private static bool IsGeocodingEnabled(IConfiguration configuration)
    {
        var options = new GeoServicesOptions();
        try
        {
            configuration.GetSection(GeoServicesOptions.SectionName).Bind(options);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return new GeoServicesPolicy(Microsoft.Extensions.Options.Options.Create(options)).Geocoding.IsEnabled;
    }
}
