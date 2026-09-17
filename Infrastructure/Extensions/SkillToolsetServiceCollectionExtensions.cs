// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the per-turn skill toolset assembly chain: query building, deterministic guarantee
/// resolution, and the assembler that combines both with retrieval, expansion and truncation. Kept
/// separate from AddLLMCoreServices so this one growing cohesive group does not keep pushing that
/// method - already the largest in this file - further past its size-guard ceiling.
/// </summary>
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class SkillToolsetServiceCollectionExtensions
{
    public static IServiceCollection AddSkillToolsetServices(this IServiceCollection services)
    {
        services.AddScoped<IRetrievalQueryBuilder, RetrievalQueryBuilder>();
        services.AddScoped<ISkillToolsetGuaranteeResolver, SkillToolsetGuaranteeResolver>();
        services.AddScoped<ISkillToolsetAssembler, SkillToolsetAssembler>();

        return services;
    }
}
