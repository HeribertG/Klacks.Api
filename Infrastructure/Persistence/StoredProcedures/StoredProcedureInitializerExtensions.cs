// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Persistence.StoredProcedures;

public static class StoredProcedureInitializerExtensions
{
    public static IServiceCollection AddStoredProcedureInitializer(this IServiceCollection services)
    {
        services.AddScoped<IStoredProcedureInitializer, StoredProcedureInitializer>();
        return services;
    }
}
