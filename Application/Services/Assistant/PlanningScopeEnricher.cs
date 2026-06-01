// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the per-client scheduling scope for an LLM turn (WP-P0.2 per-client scope). When a curated
/// scheduling skill is in scope AND PageContext.SelectedClientId is a valid id, it resolves that single
/// client's effective scheduling policy for the in-scope period (or today) and stamps it onto the
/// LLMContext. Gated on scheduling context so non-scheduling turns never trigger a policy lookup.
/// </summary>
/// <param name="ruleContextProvider">Decides whether the turn is a scheduling context (curated skill set)</param>
/// <param name="policyResolver">Resolves a client's effective scheduling policy (settings -> contract -> rule)</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class PlanningScopeEnricher : IPlanningScopeEnricher
{
    private readonly IRuleContextProvider _ruleContextProvider;
    private readonly ISchedulingPolicyResolver _policyResolver;

    public PlanningScopeEnricher(
        IRuleContextProvider ruleContextProvider,
        ISchedulingPolicyResolver policyResolver)
    {
        _ruleContextProvider = ruleContextProvider;
        _policyResolver = policyResolver;
    }

    public async Task EnrichAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        var skillNames = context.AvailableFunctions.Select(f => f.Name).ToList();
        if (!_ruleContextProvider.IsSchedulingContext(skillNames))
        {
            return;
        }

        var selectedClientId = context.PageContext?.SelectedClientId;
        if (string.IsNullOrWhiteSpace(selectedClientId) || !Guid.TryParse(selectedClientId, out var clientId))
        {
            return;
        }

        var date = ParsePeriodFromOrToday(context.PageContext?.SelectedPeriodFrom);
        context.ScopedClientPolicy = await _policyResolver.GetForClientAsync(clientId, date);
    }

    private static DateOnly ParsePeriodFromOrToday(string? periodFrom)
        => DateOnly.TryParse(periodFrom, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);
}
