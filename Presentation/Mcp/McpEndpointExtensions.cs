// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the MCP server (Streamable HTTP, stateless) into the ASP.NET Core pipeline and bridges
/// tools/list and tools/call to the Klacks skill registry and executor under JWT bearer or
/// personal access token auth, plus resources/list and resources/read to the shared
/// Klacks.Docs documentation.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Klacks.Api.Presentation.Mcp;

public static class McpEndpointExtensions
{
    public static IServiceCollection AddKlacksMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<IMcpSkillExposurePolicy, McpSkillExposurePolicy>();
        services.AddSingleton<IMcpToolCatalog, McpToolCatalog>();
        services.AddSingleton<IMcpResourceCatalog, McpResourceCatalog>();
        services.AddScoped<IMcpSkillCallHandler, McpSkillCallHandler>();

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, PatAuthenticationHandler>(PatConstants.SchemeName, configureOptions: null);

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
            .WithCallToolHandler(HandleCallToolAsync)
            .WithListResourcesHandler(HandleListResourcesAsync)
            .WithReadResourceHandler(HandleReadResourceAsync);

        return services;
    }

    public static IEndpointConventionBuilder MapKlacksMcp(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapMcp(McpServerConstants.RoutePattern)
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, PatConstants.SchemeName)
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

    private static ValueTask<ListResourcesResult> HandleListResourcesAsync(
        RequestContext<ListResourcesRequestParams> request,
        CancellationToken cancellationToken)
    {
        var catalog = RequireServices(request).GetRequiredService<IMcpResourceCatalog>();

        return ValueTask.FromResult(new ListResourcesResult
        {
            Resources = catalog.ListResources()
        });
    }

    private static async ValueTask<ReadResourceResult> HandleReadResourceAsync(
        RequestContext<ReadResourceRequestParams> request,
        CancellationToken cancellationToken)
    {
        var catalog = RequireServices(request).GetRequiredService<IMcpResourceCatalog>();
        var uri = request.Params?.Uri
            ?? throw new InvalidOperationException("Resource read parameters are missing.");

        var result = await catalog.ReadResourceAsync(uri);

        return result ?? throw new McpProtocolException(
            $"Unknown resource URI: '{uri}'",
            McpErrorCode.ResourceNotFound);
    }

    private static IServiceProvider RequireServices<TParams>(RequestContext<TParams> request)
    {
        return request.Services
            ?? throw new InvalidOperationException("Request services are not available.");
    }
}
