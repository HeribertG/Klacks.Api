using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Klacks.Api.Infrastructure.MCP;

public static class MCPServiceExtensions
{
    public static IServiceCollection AddMCP(this IServiceCollection services, bool enableMCP = false)
    {
        if (enableMCP)
        {
            services.AddSingleton<IMCPService, MCPService>();
            services.AddHostedService<MCPServer>();
        }
        
        return services;
    }
}