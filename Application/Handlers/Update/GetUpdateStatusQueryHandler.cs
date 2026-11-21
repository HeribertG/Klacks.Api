// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns the running version, the availability decision against the configured channel's manifest,
/// the currently active Klacks update/rollback operation (if any) and the most recent one. Third-party
/// operations (e.g. Whisper installs) are excluded here; they still appear in the full history.
/// An update the release carries no signed artifact for (Docker vs. native runtime identifier) is reported as
/// NotAvailableForInstallation instead of available. The PostgreSQL engine upgrade facts (running and offered major, whether
/// it can be started and why not) come from IEngineUpgradePreflight against the same manifest, so the card offers exactly
/// what the engine upgrade command accepts. When those facts cannot be read (for example the server version query fails),
/// the card still answers: the failure is logged as a warning and the engine part reads currentMajor 0, no offered major,
/// cannot start and no reason. Read-only; never enqueues work.
/// </summary>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers.Update;
using Klacks.Api.Application.Queries.Update;
using Klacks.Api.Application.Services.Update;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Domain.Services.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Update;

public class GetUpdateStatusQueryHandler : IRequestHandler<GetUpdateStatusQuery, UpdateStatusResult>
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IUpdateManifestReader _manifestReader;
    private readonly IUpdateAvailabilityEvaluator _evaluator;
    private readonly ISettingsReader _settingsReader;
    private readonly IInstallationIdentityProvider _installationIdentityProvider;
    private readonly IEngineUpgradePreflight _engineUpgradePreflight;
    private readonly ILogger<GetUpdateStatusQueryHandler> _logger;

    public GetUpdateStatusQueryHandler(
        IUpdateHistoryRepository repository,
        IUpdateManifestReader manifestReader,
        IUpdateAvailabilityEvaluator evaluator,
        ISettingsReader settingsReader,
        IInstallationIdentityProvider installationIdentityProvider,
        IEngineUpgradePreflight engineUpgradePreflight,
        ILogger<GetUpdateStatusQueryHandler> logger)
    {
        _repository = repository;
        _manifestReader = manifestReader;
        _evaluator = evaluator;
        _settingsReader = settingsReader;
        _installationIdentityProvider = installationIdentityProvider;
        _engineUpgradePreflight = engineUpgradePreflight;
        _logger = logger;
    }

    public async Task<UpdateStatusResult> Handle(GetUpdateStatusQuery request, CancellationToken cancellationToken)
    {
        var currentVersion = new SemanticVersion(MyVersion.Major, MyVersion.Minor, MyVersion.Patch);

        var active = await _repository.GetActiveOperationAsync(cancellationToken);
        if (active is not null && !KlacksSelfUpdateTypes.All.Contains(active.OperationType))
        {
            active = null;
        }

        var last = await _repository.GetLatestByTypesAsync(KlacksSelfUpdateTypes.All, cancellationToken);

        var channel = await ResolveChannelAsync();
        var manifest = await _manifestReader.GetManifestAsync(channel, cancellationToken);
        var availability = manifest is null
            ? null
            : UpdateArtifactSelector.RestrictToInstallation(
                _evaluator.Evaluate(currentVersion, manifest), manifest, _installationIdentityProvider.Get());
        var engineUpgrade = await ReadEngineUpgradeAsync(manifest, cancellationToken);

        return new UpdateStatusResult
        {
            CurrentVersion = currentVersion.ToString(),
            Availability = availability,
            ActiveOperation = active is null ? null : UpdateHistoryMapper.ToItem(active),
            LastOperation = last is null ? null : UpdateHistoryMapper.ToItem(last),
            CurrentMajor = engineUpgrade.CurrentMajor,
            AvailableMajor = engineUpgrade.AvailableMajor,
            CanStartEngineUpgrade = engineUpgrade is { Artifact: not null, BlockedReason: null },
            EngineUpgradeBlockedReason = engineUpgrade.BlockedReason?.ToString(),
        };
    }

    private async Task<EngineUpgradePreflightResult> ReadEngineUpgradeAsync(
        UpdateManifest? manifest, CancellationToken cancellationToken)
    {
        try
        {
            return await _engineUpgradePreflight.CheckAsync(manifest, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Reading the PostgreSQL engine upgrade facts failed; the status shows the engine upgrade as unavailable.");
            return new EngineUpgradePreflightResult(0, null, null, null);
        }
    }

    private async Task<UpdateChannel> ResolveChannelAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingsConstants.UPDATE_CHANNEL);
        return Enum.TryParse<UpdateChannel>(setting?.Value, ignoreCase: true, out var channel)
            ? channel
            : UpdateChannel.Stable;
    }
}
