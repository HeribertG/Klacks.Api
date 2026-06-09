// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads and advances the Klacksy first-run setup tour state stored in the ONBOARDING_STATE setting.
/// The tour is offered only on a fresh install (the seed writes a "pending" marker), once a live LLM
/// provider exists, and only to admins. Tolerates a bare status string or a JSON state in the setting.
/// @param settingsReader - reads the ONBOARDING_STATE setting (read path)
/// @param settingsRepository - upserts the ONBOARDING_STATE setting (write path)
/// @param unitOfWork - commits the persisted state change
/// @param llmRepository - used to detect whether any enabled, keyed LLM provider is live
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using System.Text.Json;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using SettingsModel = Klacks.Api.Domain.Models.Settings.Settings;

namespace Klacks.Api.Application.Services.Assistant;

public class OnboardingService : IOnboardingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISettingsReader _settingsReader;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILLMRepository _llmRepository;

    public OnboardingService(
        ISettingsReader settingsReader,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ILLMRepository llmRepository)
    {
        _settingsReader = settingsReader;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _llmRepository = llmRepository;
    }

    public async Task<OnboardingResource?> GetStateAsync(IReadOnlyList<string> userRights, CancellationToken cancellationToken = default)
    {
        if (!IsAdmin(userRights))
        {
            return null;
        }

        var setting = await _settingsReader.GetSetting(SettingsConstants.ONBOARDING_STATE);
        if (setting == null)
        {
            return null;
        }

        return await BuildResourceAsync(ParseState(setting.Value), cancellationToken);
    }

    public async Task<OnboardingResource?> UpdateStateAsync(string? status, string? completedStation, IReadOnlyList<string> userRights, CancellationToken cancellationToken = default)
    {
        if (!IsAdmin(userRights))
        {
            return null;
        }

        var setting = await _settingsReader.GetSetting(SettingsConstants.ONBOARDING_STATE);
        if (setting == null)
        {
            return null;
        }

        var state = ParseState(setting.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            state.Status = status;
        }

        if (!string.IsNullOrWhiteSpace(completedStation) && !state.CompletedStations.Contains(completedStation))
        {
            state.CompletedStations.Add(completedStation);
        }

        setting.Value = JsonSerializer.Serialize(state, JsonOptions);
        await _settingsRepository.PutSetting(setting);
        await _unitOfWork.CompleteAsync();

        return await BuildResourceAsync(state, cancellationToken);
    }

    private async Task<OnboardingResource> BuildResourceAsync(OnboardingState state, CancellationToken cancellationToken)
    {
        var resolved = state.Status is OnboardingStatus.Dismissed or OnboardingStatus.Completed;
        var shouldOffer = state.Status == OnboardingStatus.Pending && await IsLlmLiveAsync(cancellationToken);

        return new OnboardingResource
        {
            ShouldOffer = shouldOffer,
            ShowCard = !resolved,
            Status = state.Status,
            CompletedStations = state.CompletedStations,
        };
    }

    private async Task<bool> IsLlmLiveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var providers = await _llmRepository.GetProvidersAsync();
        return providers.Any(p => p.IsEnabled && p.HasApiKey);
    }

    private static OnboardingState ParseState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new OnboardingState();
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<OnboardingState>(trimmed, JsonOptions);
                if (parsed != null)
                {
                    parsed.Status = string.IsNullOrWhiteSpace(parsed.Status) ? OnboardingStatus.Pending : parsed.Status;
                    parsed.CompletedStations ??= new();
                    return parsed;
                }
            }
            catch (JsonException)
            {
                // Fall through to bare-status handling below.
            }
        }

        return new OnboardingState { Status = trimmed };
    }

    private static bool IsAdmin(IReadOnlyList<string> userRights)
    {
        return userRights.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase);
    }
}
