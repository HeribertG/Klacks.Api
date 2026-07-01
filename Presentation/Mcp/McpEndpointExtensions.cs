// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the MCP server (Streamable HTTP, stateless) into the ASP.NET Core pipeline and bridges
/// tools/list and tools/call to the Klacks skill registry and executor under JWT bearer or
/// personal access token auth, plus resources/list and resources/read to the shared
/// Klacks.Docs documentation and prompts/list and prompts/get to the guided workflow prompt catalog.
/// </summary>

using System.Threading.RateLimiting;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Klacks.Api.Presentation.Mcp;

public static class McpEndpointExtensions
{
    private const string AnonymousPartitionKey = "anonymous";

    public static IServiceCollection AddKlacksMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<IMcpSkillExposurePolicy, McpSkillExposurePolicy>();
        services.AddSingleton<IMcpToolCatalog, McpToolCatalog>();
        services.AddSingleton<IMcpResourceCatalog, McpResourceCatalog>();
        services.AddScoped<IMcpPromptCatalog, McpPromptCatalog>();
        services.AddScoped<IMcpSkillCallHandler, McpSkillCallHandler>();

        services.AddOptions<McpPublicEndpointOptions>()
            .BindConfiguration(McpPublicEndpointOptions.SectionName);

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, PatAuthenticationHandler>(PatConstants.SchemeName, configureOptions: null)
            .AddMcp();

        services.AddOptions<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<McpPublicEndpointOptions>>((options, publicEndpoint) =>
            {
                var baseUrl = publicEndpoint.Value.NormalizedPublicBaseUrl;
                options.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = baseUrl + McpServerConstants.RoutePattern,
                    AuthorizationServers = { baseUrl },
                    ScopesSupported = { OAuthConstants.McpToolsScope }
                };
            });

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
            .WithReadResourceHandler(HandleReadResourceAsync)
            .WithListPromptsHandler(HandleListPromptsAsync)
            .WithGetPromptHandler(HandleGetPromptAsync);

        services.Configure<RateLimiterOptions>(options =>
            options.AddPolicy(RateLimitingPolicies.Mcp, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User?.Identity?.Name
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? AnonymousPartitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.McpPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    })));

        return services;
    }

    public static IEndpointConventionBuilder MapKlacksMcp(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapMcp(McpServerConstants.RoutePattern)
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    PatConstants.SchemeName,
                    McpAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .RequireRateLimiting(RateLimitingPolicies.Mcp);
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

    private static async ValueTask<ListPromptsResult> HandleListPromptsAsync(
        RequestContext<ListPromptsRequestParams> request,
        CancellationToken cancellationToken)
    {
        var catalog = RequireServices(request).GetRequiredService<IMcpPromptCatalog>();

        return new ListPromptsResult
        {
            Prompts = await catalog.ListPromptsAsync(cancellationToken)
        };
    }

    private static async ValueTask<GetPromptResult> HandleGetPromptAsync(
        RequestContext<GetPromptRequestParams> request,
        CancellationToken cancellationToken)
    {
        var catalog = RequireServices(request).GetRequiredService<IMcpPromptCatalog>();
        var parameters = request.Params
            ?? throw new InvalidOperationException("Prompt request parameters are missing.");

        return await catalog.GetPromptAsync(parameters.Name, parameters.Arguments, cancellationToken);
    }

    private static IServiceProvider RequireServices<TParams>(RequestContext<TParams> request)
    {
        return request.Services
            ?? throw new InvalidOperationException("Request services are not available.");
    }
}
