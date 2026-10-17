// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Status model of the per-account intro tour. No row = new account = offer ("pending"). "offered" is an event: it
/// stamps last_offered_at, keeps a new account offerable (a toast timeout reports nothing and changes nothing) and
/// turns a legacy offer into legacy_offered, which is never offered again. An explicit "later" counts up and the
/// second one dismisses; on a legacy offer "later" ends the single offer as well. "in_progress" arriving from
/// completed, dismissed or legacy_offered is a restart (the user asked for the intro again): finished stations and the
/// snooze count are cleared so the tour starts from its first station; from any other state it resumes and keeps the
/// stations.
/// </summary>
/// <param name="repository">Per-account state rows</param>
/// <param name="unitOfWork">Commits the staged change</param>
/// <param name="llmRepository">Providers for the LlmLive flag of the slice</param>
/// <param name="timeProvider">Clock for the UTC timestamps</param>

using System.Text.RegularExpressions;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public partial class IntroTourStateService : IIntroTourStateService
{
    private static readonly HashSet<string> ReportableStatuses = new(StringComparer.Ordinal)
    {
        IntroTourDefaults.OfferedEvent,
        IntroTourStatus.Snoozed,
        IntroTourStatus.InProgress,
        IntroTourStatus.Completed,
        IntroTourStatus.Dismissed,
    };

    private readonly IUserIntroTourStateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILLMRepository _llmRepository;
    private readonly TimeProvider _timeProvider;

    public IntroTourStateService(
        IUserIntroTourStateRepository repository,
        IUnitOfWork unitOfWork,
        ILLMRepository llmRepository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _llmRepository = llmRepository;
        _timeProvider = timeProvider;
    }

    public async Task<OnboardingResource?> GetStateAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var state = await _repository.GetNoTrackingAsync(userId, cancellationToken)
            ?? new UserIntroTourState { UserId = userId };

        return await BuildResourceAsync(state, cancellationToken);
    }

    public async Task<OnboardingResource> ApplyAsync(string userId, string? status, string? completedStation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidRequestException("The intro tour state can only be saved for a signed-in user.");
        }

        var normalizedStatus = NormalizeStatus(status);
        var normalizedStation = NormalizeStation(completedStation);

        if (normalizedStatus == null && normalizedStation == null)
        {
            return (await GetStateAsync(userId, cancellationToken))!;
        }

        var state = await LoadForUpdateAsync(userId, cancellationToken);

        if (normalizedStatus != null)
        {
            ApplyStatus(state, normalizedStatus);
        }

        if (normalizedStation != null)
        {
            AddStation(state, normalizedStation);
        }

        state.UpdateTime = UtcNow();
        await _unitOfWork.CompleteAsync();

        return await BuildResourceAsync(state, cancellationToken);
    }

    public async Task CloseAfterSetupAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var state = await LoadForUpdateAsync(userId, cancellationToken);
        if (state.Status is IntroTourStatus.Completed or IntroTourStatus.Dismissed)
        {
            return;
        }

        state.Status = IntroTourStatus.Completed;
        state.UpdateTime = UtcNow();
        await _unitOfWork.CompleteAsync();
    }

    private async Task<UserIntroTourState> LoadForUpdateAsync(string userId, CancellationToken cancellationToken)
    {
        await _repository.EnsureExistsAsync(userId, IntroTourStatus.Pending, UtcNow(), cancellationToken);
        return await _repository.GetForUpdateAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"Intro tour state for user {userId} vanished right after it was ensured.");
    }

    private void ApplyStatus(UserIntroTourState state, string status)
    {
        switch (status)
        {
            case IntroTourDefaults.OfferedEvent:
                state.LastOfferedAt = UtcNow();
                if (state.Status == IntroTourStatus.LegacyOffer)
                {
                    state.Status = IntroTourStatus.LegacyOffered;
                }

                break;

            case IntroTourStatus.Snoozed:
                ApplySnooze(state);
                break;

            case IntroTourStatus.InProgress:
                if (IsRestart(state.Status))
                {
                    state.CompletedStations = new List<string>();
                    state.SnoozeCount = 0;
                }

                state.Status = IntroTourStatus.InProgress;
                break;

            default:
                state.Status = status;
                break;
        }
    }

    private static void ApplySnooze(UserIntroTourState state)
    {
        if (state.Status is IntroTourStatus.LegacyOffer or IntroTourStatus.LegacyOffered)
        {
            state.Status = IntroTourStatus.LegacyOffered;
            return;
        }

        if (state.Status is IntroTourStatus.Completed or IntroTourStatus.Dismissed)
        {
            return;
        }

        state.SnoozeCount++;
        state.Status = state.SnoozeCount >= IntroTourDefaults.SnoozeLimit
            ? IntroTourStatus.Dismissed
            : IntroTourStatus.Snoozed;
    }

    private static bool IsRestart(string currentStatus)
    {
        return currentStatus is IntroTourStatus.Completed or IntroTourStatus.Dismissed or IntroTourStatus.LegacyOffered;
    }

    private static void AddStation(UserIntroTourState state, string station)
    {
        if (state.CompletedStations.Contains(station, StringComparer.Ordinal))
        {
            return;
        }

        if (state.CompletedStations.Count >= IntroTourDefaults.MaxCompletedStations)
        {
            throw new InvalidRequestException("The intro tour already holds the maximum number of completed stations.");
        }

        state.CompletedStations = new List<string>(state.CompletedStations) { station };
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToLowerInvariant();
        if (!ReportableStatuses.Contains(normalized))
        {
            throw new InvalidRequestException("Unknown intro tour status.");
        }

        return normalized;
    }

    private static string? NormalizeStation(string? station)
    {
        if (string.IsNullOrWhiteSpace(station))
        {
            return null;
        }

        var trimmed = station.Trim();
        if (trimmed.Length > IntroTourDefaults.MaxStationIdLength || !StationIdPattern().IsMatch(trimmed))
        {
            throw new InvalidRequestException("Malformed intro tour station id.");
        }

        return trimmed;
    }

    private async Task<OnboardingResource> BuildResourceAsync(UserIntroTourState state, CancellationToken cancellationToken)
    {
        return new OnboardingResource
        {
            Kind = OnboardingKind.Intro,
            ShouldOffer = state.Status is IntroTourStatus.Pending or IntroTourStatus.LegacyOffer or IntroTourStatus.Snoozed,
            LlmLive = await IsLlmLiveAsync(cancellationToken),
            ShowCard = state.Status == IntroTourStatus.InProgress,
            Status = state.Status,
            CompletedStations = state.CompletedStations.ToList(),
            IsLegacyOffer = state.Status == IntroTourStatus.LegacyOffer,
            SnoozeCount = state.SnoozeCount,
            LastOfferedAt = state.LastOfferedAt,
        };
    }

    private async Task<bool> IsLlmLiveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var providers = await _llmRepository.GetProvidersAsync();
        return LlmProviderAvailability.AnyLive(providers);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex StationIdPattern();
}
