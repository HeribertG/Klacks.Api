// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Accept rule for scenarios that carry variant keys (virtual shifts): accepting such a scenario turns the key shifts into
/// real ones, so it needs the right to edit shifts and every key must still be Active. A scenario without keys passes
/// without any rights query. The rights come only from the current request's principal: a caller without a session (Slack
/// bridge, plan steps, scheduled tasks, automatic period commit) holds the planner floor and is refused. The rights are
/// checked before everything else, so a refused caller learns nothing about the keys. A key of a group the caller may not see
/// refuses the accept exactly like a missing scenario, before the consumed keys, so a hidden key does not reveal its state:
/// keys reach a scenario through its chain too, so the visibility of the scenario says nothing about each key.
/// </summary>
/// <param name="variantRepository">Reads the live keys the scenario carries</param>
/// <param name="userService">Gives the granular rights of the current caller</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the group of a key</param>
/// <param name="scenarioId">Row id of the scenario, named in the refusal of a hidden key</param>
/// <param name="token">Token of the scenario to accept</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Services.ScenarioVariants;

public sealed class ScenarioVariantAcceptGate
{
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IUserService _userService;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public ScenarioVariantAcceptGate(
        IScenarioVariantRepository variantRepository,
        IUserService userService,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _variantRepository = variantRepository;
        _userService = userService;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task EnsureAcceptAllowedAsync(Guid scenarioId, Guid token, CancellationToken ct)
    {
        var variants = await _variantRepository.GetVariantsOfScenarioAsync(token, ct);
        if (variants.Count == 0)
        {
            return;
        }

        if (!Permissions.HasPermission(_userService.GetRights(), Permissions.CanEditShifts))
        {
            throw new ForbiddenException(ScenarioAcceptDefaults.VariantAcceptForbiddenMessage);
        }

        foreach (var variant in variants)
        {
            if (!await _groupVisibilityGuard.IsScenarioVisibleAsync(variant, ct))
            {
                throw new KeyNotFoundException(string.Format(ScenarioGroupVisibility.ScenarioNotFoundFormat, scenarioId));
            }
        }

        var consumed = variants
            .Where(v => v.Status != AnalyseScenarioStatus.Active)
            .Select(v => v.Name)
            .Order(StringComparer.Ordinal)
            .ToList();
        if (consumed.Count > 0)
        {
            throw new ScenarioVariantConsumedException(consumed);
        }
    }
}
