// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stores the administrator's consent to the licence of the local voice model and enqueues a TtsInstall hand-off row
/// for the out-of-process updater (TargetVersion = model alias, ArtifactRef = Klacks.Stt model id). Rejected without
/// consent or with another licence version, and while any update operation is active. The consent (user id, UTC time,
/// licence version) is written to three settings and recorded in a structured audit log line before the row is queued.
/// </summary>
/// <param name="settingsRepository">Writes the three consent settings</param>
/// <param name="unitOfWork">Commits the consent settings</param>
/// <param name="updateHistoryRepository">Checks the active operation and stores the TtsInstall row</param>
/// <param name="timeProvider">Supplies the consent time and the request time of the row</param>
/// <param name="logger">Writes the consent audit line</param>
using System.Globalization;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class InstallLocalTtsCommandHandler : IRequestHandler<InstallLocalTtsCommand, LocalTtsInstallResult>
{
    private const string RoundTripFormat = "O";

    private const string AuditMessage =
        "Local voice licence audit: licence {LicenseVersion} accepted by {ActorUserId} at {AcceptedAt}";

    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUpdateHistoryRepository _updateHistoryRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InstallLocalTtsCommandHandler> _logger;

    public InstallLocalTtsCommandHandler(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        IUpdateHistoryRepository updateHistoryRepository,
        TimeProvider timeProvider,
        ILogger<InstallLocalTtsCommandHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _updateHistoryRepository = updateHistoryRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<LocalTtsInstallResult> Handle(InstallLocalTtsCommand request, CancellationToken cancellationToken)
    {
        if (!request.AcceptLicense)
        {
            return Rejected(LocalTtsInstallOutcome.InvalidConsent, LocalTtsReasons.LicenseNotAccepted);
        }

        if (!string.Equals(request.LicenseVersion, LocalTtsConstants.LicenseVersion, StringComparison.Ordinal))
        {
            return Rejected(LocalTtsInstallOutcome.InvalidConsent, LocalTtsReasons.LicenseVersionMismatch);
        }

        var active = await _updateHistoryRepository.GetActiveOperationAsync(cancellationToken);
        if (active is not null)
        {
            return Rejected(
                LocalTtsInstallOutcome.Conflict,
                active.OperationType == UpdateOperationType.TtsInstall
                    ? LocalTtsReasons.InstallAlreadyPending
                    : UpdateReasons.OperationInProgress);
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var acceptedAt = nowUtc.ToString(RoundTripFormat, CultureInfo.InvariantCulture);

        await _settingsRepository.UpsertSettingAsync(SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_BY, request.RequestedBy);
        await _settingsRepository.UpsertSettingAsync(SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_AT, acceptedAt);
        await _settingsRepository.UpsertSettingAsync(SettingsConstants.ASSISTANT_TTS_LOCAL_LICENSE_VERSION, LocalTtsConstants.LicenseVersion);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation(AuditMessage, LocalTtsConstants.LicenseVersion, request.RequestedBy, acceptedAt);

        var entry = new UpdateHistory
        {
            Id = Guid.NewGuid(),
            OperationType = UpdateOperationType.TtsInstall,
            Status = UpdateOperationStatus.Pending,
            Channel = UpdateChannel.Stable,
            TargetVersion = LocalTtsConstants.ModelAlias,
            ArtifactRef = LocalTtsConstants.ModelId,
            ContainsMigrations = false,
            RequestedBy = request.RequestedBy,
            RequestedAt = nowUtc,
        };

        try
        {
            await _updateHistoryRepository.AddAsync(entry, cancellationToken);
        }
        catch (DatabaseUpdateException)
        {
            return Rejected(LocalTtsInstallOutcome.Conflict, UpdateReasons.OperationInProgress);
        }
        catch (ConcurrencyException)
        {
            return Rejected(LocalTtsInstallOutcome.Conflict, UpdateReasons.OperationInProgress);
        }

        return new LocalTtsInstallResult(LocalTtsInstallOutcome.Enqueued, entry.Id, LocalTtsReasons.InstallEnqueued);
    }

    private static LocalTtsInstallResult Rejected(LocalTtsInstallOutcome outcome, string reason) => new(outcome, null, reason);
}
