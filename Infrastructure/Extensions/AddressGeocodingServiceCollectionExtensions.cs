// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the address geocoding: repositories of the per-address state and the locality cache, the filler, the
/// system-function service, the privacy setting reader, the backfill runner and, when BackgroundServices:AddressGeocoding
/// is on and the GeoServices configuration enables geocoding (same evaluation as IGeoServicesPolicy), the background
/// worker. AddressGeocodingBackfillAvailability tells the service whether requests are drained on this instance.
/// </summary>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Application.Services.Geocoding;
using Klacks.Api.Domain.Models.Settings.GeoServices;
using Klacks.Api.Domain.Services.GeoServices;
using Klacks.Api.Infrastructure.Repositories.Geocoding;
using Klacks.Api.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klacks.Api.Infrastructure.Extensions;

public static class AddressGeocodingServiceCollectionExtensions
{
    public static IServiceCollection AddAddressGeocodingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAddressGeocodingStateRepository, AddressGeocodingStateRepository>();
        services.AddScoped<IGeocodedLocalityRepository, GeocodedLocalityRepository>();
        services.AddScoped<IAddressGeocodingFiller, AddressGeocodingFiller>();
        services.AddScoped<IAddressGeocodingService, AddressGeocodingService>();
        services.AddScoped<IAddressGeocodingSettings, AddressGeocodingSettings>();
        services.AddScoped<IAddressGeocodingBackfillRunner, AddressGeocodingBackfillRunner>();

        var backgroundOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();
        var backfillActive = backgroundOptions.AddressGeocoding && IsGeocodingEnabled(configuration);

        services.AddSingleton(new AddressGeocodingBackfillAvailability(backfillActive));
        if (backfillActive)
        {
            services.AddHostedService<AddressGeocodingBackfillService>();
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
