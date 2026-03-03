// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.MCP;

public static class MCPServiceExtensions
{
    public static IServiceCollection AddMCP(this IServiceCollection services, bool enableMCP = false)
    {
        if (enableMCP)
        {
            services.AddSingleton<IMCPService, MCPService>();
        }
        
        return services;
    }
}