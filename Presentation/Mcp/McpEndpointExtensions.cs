// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the MCP server (Streamable HTTP, stateless) into the ASP.NET Core pipeline and bridges
/// tools/list and tools/call to the Klacks skill registry and executor under JWT bearer auth.
/// </summary>

using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Klacks.Api.Presentation.Mcp;

public static class McpEndpointExtensions
{
    public static IServiceCollection AddKlacksMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<IMcpSkillExposurePolicy, McpSkillExposurePolicy>();
        services.AddSingleton<IMcpToolCatalog, McpToolCatalog>();
        services.AddScoped<IMcpSkillCallHandler, McpSkillCallHandler>();

        services.AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = McpServerConstants.ServerName,
                    Title = McpServerConstants.ServerTitle,
                    Version = McpServerConstants.ServerVersion
                };
                options.ServerInstructions = McpServerConstants.ServerInstructions;
            })
            .WithHttpTransport(transport => transport.Stateless = true)
            .WithListToolsHandler(HandleListToolsAsync)
            .WithCallToolHandler(HandleCallToolAsync);

        return services;
    }

    public static IEndpointConventionBuilder MapKlacksMcp(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapMcp(McpServerConstants.RoutePattern)
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
    }

    private static ValueTask<ListToolsResult> HandleListToolsAsync(
        RequestContext<ListToolsRequestParams> request,
        CancellationToken cancellationToken)
    {
        var catalog = RequireServices(request).GetRequiredService<IMcpToolCatalog>();
        var userContext = McpUserContextReader.Read(request.User);

        return ValueTask.FromResult(new ListToolsResult
        {
            Tools = catalog.GetToolsForUser(userContext.Permissions)
        });
    }

    private static async ValueTask<CallToolResult> HandleCallToolAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var handler = RequireServices(request).GetRequiredService<IMcpSkillCallHandler>();
        var parameters = request.Params
            ?? throw new InvalidOperationException("Tool call parameters are missing.");

        return await handler.HandleAsync(parameters, request.User, cancellationToken);
    }

    private static IServiceProvider RequireServices<TParams>(RequestContext<TParams> request)
    {
        return request.Services
            ?? throw new InvalidOperationException("Request services are not available.");
    }
}
