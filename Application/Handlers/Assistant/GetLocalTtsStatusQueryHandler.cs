// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the local voice status from the settings store and the update hand-off queue. Installed means the updater
/// wrote ASSISTANT_TTS_LOCAL_BASE_URL; the licence counts as accepted only for the current licence version, so a new
/// licence asks the administrator again; LastError is the message of the latest TtsInstall row when it failed.
/// </summary>
/// <param name="settingsRepository">Reads the local voice address and the stored licence consent</param>
/// <param name="updateHistoryRepository">Supplies the active and the latest TtsInstall operation</param>
using Klacks.Api.Application.Constants;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetLocalTtsStatusQueryHandler : IRequestHandler<GetLocalTtsStatusQuery, LocalTtsStatus>
{
    private static readonly UpdateOperationType[] TtsTypes = [UpdateOperationType.TtsInstall];

    private static readonly string[] SettingKeys =
    [
        SettingsConstants.ASSISTANT_TTS_LOCAL_BASE_URL,
        SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_BY,
        SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_AT,
        SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_VERSION,
    ];

    private readonly ISettingsRepository _settingsRepository;
    private readonly IUpdateHistoryRepository _updateHistoryRepository;

    public GetLocalTtsStatusQueryHandler(
        ISettingsRepository settingsRepository,
        IUpdateHistoryRepository updateHistoryRepository)
    {
        _settingsRepository = settingsRepository;
        _updateHistoryRepository = updateHistoryRepository;
    }

    public async Task<LocalTtsStatus> Handle(GetLocalTtsStatusQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetSettingsByTypesAsync(SettingKeys, cancellationToken);

        var installed = Read(settings, SettingsConstants.ASSISTANT_TTS_LOCAL_BASE_URL) is not null;
        var licenseAccepted =
            string.Equals(Read(settings, SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_VERSION), LocalTtsConstants.LicenseVersion, StringComparison.Ordinal)
            && Read(settings, SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_BY) is not null;
        var licenseAcceptedAt = licenseAccepted ? Read(settings, SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_AT) : null;

        var active = await _updateHistoryRepository.GetActiveOperationAsync(cancellationToken);
        var pending = active?.OperationType == UpdateOperationType.TtsInstall;

        var latest = await _updateHistoryRepository.GetLatestByTypesAsync(TtsTypes, cancellationToken);
        var lastError = latest is { Status: UpdateOperationStatus.Failed } ? latest.Message : null;

        return new LocalTtsStatus(installed, pending, lastError, licenseAccepted, licenseAcceptedAt, LocalTtsConstants.ModelSizeBytes);
    }

    private static string? Read(IReadOnlyDictionary<string, string> settings, string key) =>
        settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
