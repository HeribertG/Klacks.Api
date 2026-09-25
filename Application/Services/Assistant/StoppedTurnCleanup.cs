// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// See IStoppedTurnCleanup.
/// </summary>
/// <param name="confirmationDiscarder">Drops the confirmation tokens and proposal hints the turn issued</param>
/// <param name="usageRepository">Closes the turn's dispatched UiAction rows</param>
/// <param name="logger">Logs a failed write</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class StoppedTurnCleanup : IStoppedTurnCleanup
{
    private readonly ITurnConfirmationDiscarder _confirmationDiscarder;
    private readonly ISkillUsageRepository _usageRepository;
    private readonly ILogger<StoppedTurnCleanup> _logger;

    public StoppedTurnCleanup(
        ITurnConfirmationDiscarder confirmationDiscarder,
        ISkillUsageRepository usageRepository,
        ILogger<StoppedTurnCleanup> logger)
    {
        _confirmationDiscarder = confirmationDiscarder;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task CleanUpAsync(string userId, Guid turnId, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(userId, out var userGuid) && userGuid != Guid.Empty)
        {
            _confirmationDiscarder.DiscardIssuedThisTurn(userGuid);
        }

        if (turnId == Guid.Empty)
        {
            return;
        }

        try
        {
            await _usageRepository.CancelDispatchedForTurnAsync(turnId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Closing the UiAction rows of stopped turn {TurnId} failed", turnId);
        }
    }
}
