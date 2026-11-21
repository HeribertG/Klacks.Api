// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides, without writing, whether a PostgreSQL engine upgrade could be queued now. Blocking order: the manifest offers
/// no engine, no signed engine package for this installation (Docker image vs. native bundle of the own runtime
/// identifier, see EngineUpgradeArtifactSelector), the offered major is not newer than the running one, an operation of
/// any type is active, the updater has not reported the engine upgrade capability, its report is stale. The active check
/// comes before the capability checks because the updater pauses its report while an engine upgrade runs. Any newer major
/// is allowed, not only the next one. Signatures are not verified here; the updater does that before it changes anything.
/// </summary>
/// <param name="repository">Reads the active operation of the hand-off queue</param>
/// <param name="manifestReader">Fetches the channel manifest</param>
/// <param name="settingsReader">Reads the configured channel and the updater capability report</param>
/// <param name="installationIdentityProvider">Tells Docker from native and names the runtime identifier</param>
/// <param name="engineVersionReader">Reads the major of the running PostgreSQL server</param>
/// <param name="timeProvider">Clock for the freshness of the capability report</param>

using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Domain.Services.Update;

namespace Klacks.Api.Application.Services.Update;

public class EngineUpgradePreflight : IEngineUpgradePreflight
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IUpdateManifestReader _manifestReader;
    private readonly ISettingsReader _settingsReader;
    private readonly IInstallationIdentityProvider _installationIdentityProvider;
    private readonly IDatabaseEngineVersionReader _engineVersionReader;
    private readonly TimeProvider _timeProvider;

    public EngineUpgradePreflight(
        IUpdateHistoryRepository repository,
        IUpdateManifestReader manifestReader,
        ISettingsReader settingsReader,
        IInstallationIdentityProvider installationIdentityProvider,
        IDatabaseEngineVersionReader engineVersionReader,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _manifestReader = manifestReader;
        _settingsReader = settingsReader;
        _installationIdentityProvider = installationIdentityProvider;
        _engineVersionReader = engineVersionReader;
        _timeProvider = timeProvider;
    }

    public async Task<EngineUpgradePreflightResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var channelSetting = await _settingsReader.GetSetting(SettingsConstants.UPDATE_CHANNEL);
        var channel = Enum.TryParse<UpdateChannel>(channelSetting?.Value, ignoreCase: true, out var parsed)
            ? parsed
            : UpdateChannel.Stable;

        var manifest = await _manifestReader.GetManifestAsync(channel, cancellationToken);
        return await CheckAsync(manifest, cancellationToken);
    }

    public async Task<EngineUpgradePreflightResult> CheckAsync(
        UpdateManifest? manifest, CancellationToken cancellationToken = default)
    {
        var currentMajor = await _engineVersionReader.GetMajorVersionAsync(cancellationToken);
        var engine = manifest?.Artifacts.Postgres;
        if (engine is null)
        {
            return new EngineUpgradePreflightResult(currentMajor, null, null, EngineUpgradeBlockedReason.NoEngineInManifest);
        }

        var artifact = EngineUpgradeArtifactSelector.Select(engine, _installationIdentityProvider.Get());
        var blockedReason = await BlockedReasonAsync(engine.Major, currentMajor, artifact, cancellationToken);
        return new EngineUpgradePreflightResult(currentMajor, engine.Major, artifact, blockedReason);
    }

    private async Task<EngineUpgradeBlockedReason?> BlockedReasonAsync(
        int availableMajor, int currentMajor, SelectedUpdateArtifact? artifact, CancellationToken cancellationToken)
    {
        if (artifact is null)
        {
            return EngineUpgradeBlockedReason.NoArtifactForInstallation;
        }

        if (availableMajor <= currentMajor)
        {
            return EngineUpgradeBlockedReason.NotNewerThanCurrent;
        }

        if (await _repository.GetActiveOperationAsync(cancellationToken) is not null)
        {
            return EngineUpgradeBlockedReason.OperationInProgress;
        }

        var report = await _settingsReader.GetSetting(SettingsConstants.UPDATER_CAPABILITIES);
        return UpdaterCapabilityEvaluator.EvaluateEngineUpgrade(report?.Value, _timeProvider.GetUtcNow().UtcDateTime) switch
        {
            UpdaterCapabilityState.Supported => null,
            UpdaterCapabilityState.Stale => EngineUpgradeBlockedReason.UpdaterCapabilityStale,
            _ => EngineUpgradeBlockedReason.UpdaterCapabilityMissing,
        };
    }
}
