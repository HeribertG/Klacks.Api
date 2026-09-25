// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wires the escalation chain: its repository, the absence roster, the notifier and its handoff text service, the reply observer,
/// the chain service itself, the approval roster resolver, the approval chain starter for proactive
/// conditions and the standing-approval store that lets a granted kind skip that chain. Kept separate
/// from AddLLMCoreServices for the same reason AddSkillToolsetServices is - one cohesive group that
/// keeps growing must not push that method, already the largest in ServiceCollectionExtensions, past
/// its size-guard ceiling. Registration order of IInboundMessengerObserver is preserved by calling this
/// from the exact position the EscalationReplyObserver registration used to occupy.
/// </summary>
using Klacks.Api.Application.Services.Assistant.Conditions;
using Klacks.Api.Application.Services.Assistant.Escalation;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Repositories.Assistant;
using Klacks.Api.Infrastructure.Services.Assistant.Escalation;
using Klacks.Plugin.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

public static class EscalationServiceCollectionExtensions
{
    public static IServiceCollection AddEscalationChainServices(this IServiceCollection services)
    {
        services.AddScoped<IInboundMessengerObserver, EscalationReplyObserver>();
        services.AddScoped<IEscalationChainRepository, EscalationChainRepository>();
        services.AddScoped<IEscalationRosterService, EscalationRosterService>();
        services.AddScoped<IEscalationNotifier, EscalationNotifier>();
        services.AddScoped<IEscalationHandoffTextService, EscalationHandoffTextService>();
        services.AddScoped<IEscalationChainService, EscalationChainService>();
        services.AddScoped<ISkillPermissionGate, SkillPermissionGate>();
        services.AddScoped<IConditionApprovalRosterResolver, ConditionApprovalRosterResolver>();
        services.AddScoped<IConditionApprovalChainStarter, ConditionApprovalChainStarter>();
        services.AddScoped<IStandingApprovalRepository, StandingApprovalRepository>();

        return services;
    }
}
