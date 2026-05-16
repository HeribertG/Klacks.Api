// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for per-user trigger preferences. Lets the Klacksy Settings UI mute or snooze
/// specific kinds (e.g. mute target_hours_drift, snooze unstaffed_shift until tomorrow,
/// raise the threshold for scenario_pending to 'high' only).
/// </summary>
/// <param name="preferenceService">In-memory store for S8; persistent store in a later sprint.</param>

using System.Security.Claims;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/trigger-preferences")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AgentTriggerPreferencesController : ControllerBase
{
    private static readonly string[] KnownKinds =
    [
        AgentTriggerKinds.UnstaffedShift,
        AgentTriggerKinds.LockConflict,
        AgentTriggerKinds.TargetHoursDrift,
        AgentTriggerKinds.ScenarioPending,
        AgentTriggerKinds.PeriodCloseDue,
        AgentTriggerKinds.ContractExpiringSoon
    ];

    private readonly IAgentTriggerPreferenceService _preferenceService;

    public AgentTriggerPreferencesController(IAgentTriggerPreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    [HttpGet]
    public IActionResult ListMyPreferences()
    {
        var userId = GetCurrentUserId();
        var rows = KnownKinds.Select(kind =>
        {
            var pref = _preferenceService.GetPreference(userId, kind);
            return new TriggerPreferenceDto
            {
                TriggerKind = pref.TriggerKind,
                Muted = pref.Muted,
                SnoozedUntilUtc = pref.SnoozedUntilUtc,
                MinimumSeverity = pref.MinimumSeverity
            };
        }).ToList();
        return Ok(rows);
    }

    [HttpPut("{triggerKind}")]
    public IActionResult UpdatePreference(string triggerKind, [FromBody] UpdateTriggerPreferenceRequest request)
    {
        if (!KnownKinds.Contains(triggerKind, StringComparer.Ordinal))
        {
            return BadRequest($"Unknown trigger kind: {triggerKind}");
        }

        var userId = GetCurrentUserId();

        if (request.Muted.HasValue)
        {
            _preferenceService.Mute(userId, triggerKind, request.Muted.Value);
        }

        if (request.SnoozedUntilUtc.HasValue || request.Muted == false)
        {
            _preferenceService.Snooze(userId, triggerKind, request.SnoozedUntilUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.MinimumSeverity))
        {
            try
            {
                _preferenceService.SetMinimumSeverity(userId, triggerKind, request.MinimumSeverity);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        var updated = _preferenceService.GetPreference(userId, triggerKind);
        return Ok(new TriggerPreferenceDto
        {
            TriggerKind = updated.TriggerKind,
            Muted = updated.Muted,
            SnoozedUntilUtc = updated.SnoozedUntilUtc,
            MinimumSeverity = updated.MinimumSeverity
        });
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
