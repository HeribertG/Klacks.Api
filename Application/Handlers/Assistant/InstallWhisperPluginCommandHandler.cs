// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enqueues a Whisper plugin installation as a Pending hand-off row for the out-of-process updater.
/// TargetVersion carries the model alias (UI value), ArtifactRef the full Hugging Face model id the
/// updater passes to Speaches. Rejected when the alias is unknown or another operation is active.
/// </summary>
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Assistant;

public class InstallWhisperPluginCommandHandler : IRequestHandler<InstallWhisperPluginCommand, UpdateTriggerResult>
{
    private readonly IUpdateHistoryRepository _repository;

    public InstallWhisperPluginCommandHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateTriggerResult> Handle(InstallWhisperPluginCommand request, CancellationToken cancellationToken)
    {
        var modelId = WhisperPluginConstants.ResolveModelId(request.ModelAlias);
        if (modelId is null)
        {
            return Rejected(WhisperPluginReasons.UnknownModel);
        }

        if (await _repository.GetActiveOperationAsync(cancellationToken) is not null)
        {
            return Rejected(UpdateReasons.OperationInProgress);
        }

        var entry = new UpdateHistory
        {
            Id = Guid.NewGuid(),
            OperationType = UpdateOperationType.WhisperInstall,
            Status = UpdateOperationStatus.Pending,
            Channel = UpdateChannel.Stable,
            TargetVersion = request.ModelAlias,
            ArtifactRef = modelId,
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
            Reason = WhisperPluginReasons.InstallEnqueued,
        };
    }

    private static UpdateTriggerResult Rejected(string reason) => new() { Enqueued = false, Reason = reason };
}
