// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the employee import: file readers, the synonym catalog (loaded once from the embedded
/// resource), detection/evaluation services, repositories and the template writer. The geocoding of imported
/// addresses is registered by AddAddressGeocodingServices (the import only requests it).
/// </summary>

using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Infrastructure.Repositories.Imports;
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

        return services;
    }
}
