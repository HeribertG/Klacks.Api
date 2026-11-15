// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Wires the per-turn skill toolset assembly chain: query building, the turn-intent classifier and its
/// explain guarantee, deterministic guarantee resolution, the assembler that combines both with retrieval, expansion and truncation, the
/// correction-turn preparer both chat entry points share on top of it, and the in-turn tool search (find_tools above the
/// cap, and the per-turn hand-over of the skills it finds). Kept separate from
/// AddLLMCoreServices so this one growing cohesive group does not keep pushing that method - already
/// the largest in this file - further past its size-guard ceiling.
/// </summary>
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Application.Services.Assistant.ToolSearch;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.TurnIntent;
using Klacks.Api.Infrastructure.Services.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class SkillToolsetServiceCollectionExtensions
{
    public static IServiceCollection AddSkillToolsetServices(this IServiceCollection services)
    {
        services.AddScoped<IRetrievalQueryBuilder, RetrievalQueryBuilder>();
        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return TurnIntentClassifierOptions.FromConfiguration(
                configuration.GetValue<string>(TurnIntentDefaults.ModeConfigKey),
                configuration.GetValue(TurnIntentDefaults.TimeoutConfigKey, TurnIntentDefaults.DefaultTimeoutMilliseconds),
                configuration.GetValue(TurnIntentDefaults.VetoConfigKey, TurnIntentDefaults.DefaultVetoForcedToolCall));
        });
        services.AddSingleton<ITurnIntentHeadProvider, FileTurnIntentHeadProvider>();
        services.AddScoped<ITurnIntentClassifier, TurnIntentClassifier>();
        services.AddScoped<ITurnIntentExplainGuarantee, TurnIntentExplainGuarantee>();
        services.AddScoped<IScenarioActivityProbe, ScenarioActivityProbe>();
        services.AddScoped<ISkillToolsetGuaranteeResolver, SkillToolsetGuaranteeResolver>();
        services.AddScoped<IDraftLoopToolsetPolicy, DraftLoopToolsetPolicy>();
        services.AddScoped<ISkillToolsetAssembler, SkillToolsetAssembler>();
        services.AddScoped<ICorrectionTurnPreparer, CorrectionTurnPreparer>();
        services.AddSingleton(provider => new ToolSearchOptions(
            provider.GetRequiredService<IConfiguration>().GetValue(
                ToolSearchOptions.EnabledConfigKey, ToolSearchOptions.DefaultEnabled)));
        services.AddScoped<IToolSearchToolsetAppender, ToolSearchToolsetAppender>();
        services.AddScoped<ITurnToolsetExtension, TurnToolsetExtension>();

        return services;
    }
}
