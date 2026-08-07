// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the Whisper plugin status for the settings card from the provider row and the hand-off
/// queue: installed state and model, the active whisper operation (for progress polling), the last
/// terminal whisper operation (for result display), and whether a foreign operation blocks the queue.
/// The active operation carries StartedAt so the card can tell a claimed, running installation from
/// one that is merely queued because no updater is there to pick it up.
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetWhisperPluginStatusQueryHandler : IRequestHandler<GetWhisperPluginStatusQuery, WhisperPluginStatus>
{
    private static readonly UpdateOperationType[] WhisperTypes =
        [UpdateOperationType.WhisperInstall, UpdateOperationType.WhisperUninstall];

    private static readonly UpdateOperationStatus[] ActiveStatuses =
        [UpdateOperationStatus.Pending, UpdateOperationStatus.Running];

    private readonly ICustomSttProviderRepository _providerRepository;
    private readonly IUpdateHistoryRepository _updateHistoryRepository;

    public GetWhisperPluginStatusQueryHandler(
        ICustomSttProviderRepository providerRepository,
        IUpdateHistoryRepository updateHistoryRepository)
    {
        _providerRepository = providerRepository;
        _updateHistoryRepository = updateHistoryRepository;
    }

    public async Task<WhisperPluginStatus> Handle(GetWhisperPluginStatusQuery request, CancellationToken cancellationToken)
    {
        var status = new WhisperPluginStatus();

        var provider = await _providerRepository.GetByIdAsync(WhisperPluginConstants.ProviderId, cancellationToken);
        if (provider is { IsEnabled: true })
        {
            status.Installed = true;
            status.ModelId = provider.LanguageModel;
            status.ModelAlias = WhisperPluginConstants.ResolveModelAlias(provider.LanguageModel);
        }

        var active = await _updateHistoryRepository.GetActiveOperationAsync(cancellationToken);
        if (active is not null)
        {
            if (WhisperTypes.Contains(active.OperationType))
            {
                status.ActiveOperation = Map(active);
            }
            else
            {
                status.OtherOperationActive = true;
            }
        }

        var latest = await _updateHistoryRepository.GetLatestByTypesAsync(WhisperTypes, cancellationToken);
        if (latest is not null && !ActiveStatuses.Contains(latest.Status))
        {
            status.LastOperation = Map(latest);
        }

        return status;
    }

    private static WhisperPluginOperationInfo Map(UpdateHistory entry) => new()
    {
        Id = entry.Id,
        OperationType = entry.OperationType.ToString(),
        Status = entry.Status.ToString(),
        ModelAlias = string.IsNullOrWhiteSpace(entry.TargetVersion) ? null : entry.TargetVersion,
        Message = entry.Message,
        RequestedAt = entry.RequestedAt,
        StartedAt = entry.StartedAt,
        CompletedAt = entry.CompletedAt,
    };
}
