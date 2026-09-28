// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Persistence.StoredProcedures;

public static class StoredProcedureInitializerExtensions
{
    public static IServiceCollection AddStoredProcedureInitializer(this IServiceCollection services)
    {
        services.AddScoped<IStoredProcedureInitializer, StoredProcedureInitializer>();
        return services;
    }
}
