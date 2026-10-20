// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the database initializer together with the startup steps it runs (stored procedures,
/// identity provider secret backfill, demo order export).
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence.Seed.Demo;
using Klacks.Api.Infrastructure.Persistence.StoredProcedures;

namespace Klacks.Api.Infrastructure.Persistence;

public static class DatabaseInitializerExtensions
{
    public static IServiceCollection AddDatabaseInitializer(this IServiceCollection services)
    {
        services.AddStoredProcedureInitializer();
        services.AddScoped<IIdentityProviderSecretBackfill, IdentityProviderSecretBackfill>();
        services.AddScoped<IDemoOrderXmlGenerator, DemoOrderXmlGenerator>();
        services.AddScoped<IDemoOrderSeedFileWriter, DemoOrderSeedFileWriter>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        return services;
    }
}
