// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Reads the configured Holistic Harmonizer model and the cached vision verdict for it. Not ready when no
/// model is configured or when the model already failed the image round-trip; ready otherwise, including a
/// model that was never measured.
/// </summary>
/// <param name="settingsReader">Source of the configured Holistic Harmonizer model id.</param>
/// <param name="capabilityCache">Cached per-model verdict of the image round-trip.</param>
public sealed class HolisticHarmonizerReadinessCheck : IHolisticHarmonizerReadinessCheck
{
    public const string ModelNotConfiguredReason =
        "No model is configured for the holistic harmonization (Settings > Work & Scheduling > Holistic Harmonizer).";

    public const string ModelNotVisionCapableReasonFormat =
        "The configured model '{0}' cannot read the schedule image that the holistic harmonization needs ({1}).";

    private const string MissingCapabilityError = "no vision support";

    private readonly ISettingsReader _settingsReader;
    private readonly HolisticHarmonizerModelCapabilityCache _capabilityCache;

    public HolisticHarmonizerReadinessCheck(
        ISettingsReader settingsReader,
        HolisticHarmonizerModelCapabilityCache capabilityCache)
    {
        _settingsReader = settingsReader;
        _capabilityCache = capabilityCache;
    }

    public async Task<HolisticHarmonizerReadiness> CheckAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var modelSetting = await _settingsReader.GetSetting(Settings.HOLISTIC_HARMONIZER_LLM_MODEL);
        var modelId = modelSetting?.Value;
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return HolisticHarmonizerReadiness.NotReady(ModelNotConfiguredReason);
        }

        if (_capabilityCache.TryGet(modelId, out var isVisionCapable, out var error) && !isVisionCapable)
        {
            return HolisticHarmonizerReadiness.NotReady(
                string.Format(ModelNotVisionCapableReasonFormat, modelId, error ?? MissingCapabilityError));
        }

        return HolisticHarmonizerReadiness.Ready();
    }
}
