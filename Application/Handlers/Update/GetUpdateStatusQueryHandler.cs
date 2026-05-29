// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the running version, the availability decision against the configured channel's manifest,
/// the currently active update operation (if any) and the most recent completed operation.
/// Read-only; never enqueues work.
/// </summary>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Mappers.Update;
using Klacks.Api.Application.Queries.Update;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class GetUpdateStatusQueryHandler : IRequestHandler<GetUpdateStatusQuery, UpdateStatusResult>
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IUpdateManifestReader _manifestReader;
    private readonly IUpdateAvailabilityEvaluator _evaluator;
    private readonly ISettingsReader _settingsReader;

    public GetUpdateStatusQueryHandler(
        IUpdateHistoryRepository repository,
        IUpdateManifestReader manifestReader,
        IUpdateAvailabilityEvaluator evaluator,
        ISettingsReader settingsReader)
    {
        _repository = repository;
        _manifestReader = manifestReader;
        _evaluator = evaluator;
        _settingsReader = settingsReader;
    }

    public async Task<UpdateStatusResult> Handle(GetUpdateStatusQuery request, CancellationToken cancellationToken)
    {
        var currentVersion = new SemanticVersion(MyVersion.Major, MyVersion.Minor, MyVersion.Patch);

        var active = await _repository.GetActiveOperationAsync(cancellationToken);
        var recent = await _repository.GetRecentAsync(1, cancellationToken);
        var last = recent.Count > 0 ? recent[0] : null;

        var channel = await ResolveChannelAsync();
        var manifest = await _manifestReader.GetManifestAsync(channel, cancellationToken);
        var availability = manifest is null ? null : _evaluator.Evaluate(currentVersion, manifest);

        return new UpdateStatusResult
        {
            CurrentVersion = currentVersion.ToString(),
            Availability = availability,
            ActiveOperation = active is null ? null : UpdateHistoryMapper.ToItem(active),
            LastOperation = last is null ? null : UpdateHistoryMapper.ToItem(last),
        };
    }

    private async Task<UpdateChannel> ResolveChannelAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingsConstants.UPDATE_CHANNEL);
        return Enum.TryParse<UpdateChannel>(setting?.Value, ignoreCase: true, out var channel)
            ? channel
            : UpdateChannel.Stable;
    }
}
