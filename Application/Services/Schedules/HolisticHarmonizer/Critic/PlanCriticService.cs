// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Vision critic at the end of a Wizard 3 run. It never changes the plan: it shows a vision-capable model the finished
/// plan (anonymized: rows r00, r01, ..., no names or initials), asks which patterns a human planner would find poor,
/// and keeps only findings whose cited cells really show the claimed symbols.
///
/// Model choice: WIZARD3_CRITIC_MODEL when set. Otherwise WIZARD3_LLM_MODEL, but only while WIZARD3_MODE is "llm" -
/// WIZARD3_LLM_MODEL is seeded on every installation, and the deterministic default must not start sending plans to
/// an external model nobody chose. With neither, the review is skipped. A model the capability cache already knows
/// to be text-only is skipped too; an unknown model is simply tried (the full vision probe costs up to 90 s).
/// Every failure is reported as <see cref="PlanCriticStatus.Failed"/> and never fails the run.
/// </summary>
public sealed class PlanCriticService
{
    public const int MaxFindings = 10;
    private const string DefaultLanguage = "en";
    private const string NotConfiguredReason =
        "No critic model configured (WIZARD3_CRITIC_MODEL, or WIZARD3_LLM_MODEL with WIZARD3_MODE = llm).";
    private const string NotVisionCapableReason = "Critic model is not vision-capable: {0}";
    private const string EmptyPlanReason = "The plan has no rows or no days.";
    private const string TimeoutReason = "Critic call was cancelled or timed out.";

    private readonly IPlanCriticClient _client;
    private readonly ISettingsReader _settingsReader;
    private readonly HolisticHarmonizerModelCapabilityCache _capabilityCache;
    private readonly AnonymizedPlanImageRenderer _imageRenderer = new();
    private readonly ILogger<PlanCriticService> _logger;

    public PlanCriticService(
        IPlanCriticClient client,
        ISettingsReader settingsReader,
        HolisticHarmonizerModelCapabilityCache capabilityCache,
        ILogger<PlanCriticService> logger)
    {
        _client = client;
        _settingsReader = settingsReader;
        _capabilityCache = capabilityCache;
        _logger = logger;
    }

    public async Task<PlanCriticOutcome> ReviewAsync(HarmonyBitmap plan, string? language, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var modelId = await ResolveModelIdAsync();
        if (modelId is null)
        {
            return PlanCriticOutcome.Skipped(NotConfiguredReason);
        }

        if (_capabilityCache.TryGet(modelId, out var isVisionCapable, out var capabilityError) && !isVisionCapable)
        {
            return PlanCriticOutcome.Skipped(string.Format(NotVisionCapableReason, capabilityError), modelId);
        }

        if (plan.RowCount == 0 || plan.DayCount == 0)
        {
            return PlanCriticOutcome.Skipped(EmptyPlanReason, modelId);
        }

        try
        {
            var request = new PlanCriticRequest(
                modelId,
                _imageRenderer.Render(plan),
                AnonymizedPlanTextRenderer.Render(plan),
                string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language,
                MaxFindings);

            var response = await _client.ReviewAsync(request, cancellationToken);
            if (response.Error is not null)
            {
                _logger.LogWarning("Wizard 3 critic failed for model {ModelId}: {Error}", modelId, response.Error);
                return PlanCriticOutcome.Failed(modelId, response.Error);
            }

            var (verified, discarded) = PlanCriticFindingVerifier.Verify(plan, response.Findings, MaxFindings);
            _logger.LogInformation(
                "Wizard 3 critic finished: model={ModelId} proposed={Proposed} verified={Verified} discarded={Discarded}",
                modelId, response.Findings.Count, verified.Count, discarded);
            return PlanCriticOutcome.Completed(modelId, verified, discarded);
        }
        catch (OperationCanceledException)
        {
            return PlanCriticOutcome.Failed(modelId, TimeoutReason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wizard 3 critic threw for model {ModelId}", modelId);
            return PlanCriticOutcome.Failed(modelId, ex.Message);
        }
    }

    private async Task<string?> ResolveModelIdAsync()
    {
        var critic = (await _settingsReader.GetSetting(Settings.HOLISTIC_HARMONIZER_CRITIC_MODEL))?.Value;
        if (!string.IsNullOrWhiteSpace(critic))
        {
            return critic.Trim();
        }

        var mode = HolisticHarmonizerModes.Parse((await _settingsReader.GetSetting(Settings.HOLISTIC_HARMONIZER_MODE))?.Value);
        if (mode != HolisticHarmonizerMode.Llm)
        {
            return null;
        }

        var llmModel = (await _settingsReader.GetSetting(Settings.HOLISTIC_HARMONIZER_LLM_MODEL))?.Value;
        return string.IsNullOrWhiteSpace(llmModel) ? null : llmModel.Trim();
    }
}
