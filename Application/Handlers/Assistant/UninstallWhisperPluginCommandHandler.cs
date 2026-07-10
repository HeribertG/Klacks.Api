// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enqueues a Whisper plugin uninstallation for the out-of-process updater. Resets the active STT
/// engine to the browser engine first when it points at the Whisper provider, so speech input keeps
/// working while the container is being stopped.
/// </summary>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UninstallWhisperPluginCommandHandler : IRequestHandler<UninstallWhisperPluginCommand, UpdateTriggerResult>
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly ICustomSttProviderRepository _providerRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UninstallWhisperPluginCommandHandler(
        IUpdateHistoryRepository repository,
        ICustomSttProviderRepository providerRepository,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _providerRepository = providerRepository;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateTriggerResult> Handle(UninstallWhisperPluginCommand request, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.GetByIdAsync(WhisperPluginConstants.ProviderId, cancellationToken);
        if (provider is null || !provider.IsEnabled)
        {
            return Rejected(WhisperPluginReasons.NotInstalled);
        }

        if (await _repository.GetActiveOperationAsync(cancellationToken) is not null)
        {
            return Rejected(UpdateReasons.OperationInProgress);
        }

        await ResetSttEngineIfWhisperAsync();

        var entry = new UpdateHistory
        {
            Id = Guid.NewGuid(),
            OperationType = UpdateOperationType.WhisperUninstall,
            Status = UpdateOperationStatus.Pending,
            Channel = UpdateChannel.Stable,
            TargetVersion = WhisperPluginConstants.ResolveModelAlias(provider.LanguageModel) ?? string.Empty,
            ContainsMigrations = false,
            RequestedBy = request.RequestedBy,
            RequestedAt = DateTime.UtcNow,
        };

        try
        {
            await _repository.AddAsync(entry, cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Rejected(UpdateReasons.OperationInProgress);
        }

        return new UpdateTriggerResult
        {
            Enqueued = true,
            OperationId = entry.Id,
            Reason = WhisperPluginReasons.UninstallEnqueued,
        };
    }

    private async Task ResetSttEngineIfWhisperAsync()
    {
        var whisperEngine = $"{SttProviderConstants.CustomProviderPrefix}{WhisperPluginConstants.ProviderId}";
        var setting = await _settingsRepository.GetSetting(SettingsConstants.ASSISTANT_STT_ENGINE);
        if (setting is null || !string.Equals(setting.Value, whisperEngine, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        setting.Value = SttProviderConstants.Browser;
        await _settingsRepository.PutSetting(setting);
        await _unitOfWork.CompleteAsync();
    }

    private static UpdateTriggerResult Rejected(string reason) => new() { Enqueued = false, Reason = reason };
}
