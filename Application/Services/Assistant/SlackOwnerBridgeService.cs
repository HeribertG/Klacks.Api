// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges inbound Slack messages from the owner's registered self-alias to Klacksy's normal LLM
/// conversation, and sends the reply back to the same Slack DM. There is no live HTTP session behind
/// an inbound Slack message, so the executing AppUser is resolved the same way
/// EmailActionOrchestrator resolves "the executing admin" for received emails: the lowest GUID among
/// IPlanningAudienceResolver's admin users. Every attempted message advances the watermark exactly
/// once, success or failure — a transient failure is logged but never retried, because retrying would
/// require re-attempting (and therefore re-replying to) any already-successful messages that came
/// after it in the same batch, and a silently duplicated reply is a worse outcome for this feature
/// than an occasional silently dropped one.
/// Deliberately dispatches with Roles.Authorised rights rather than the admin's full Roles.Admin set:
/// AutonomyDefaults.DefaultLevel is Autonomous, so the autonomy gate alone would not stop a
/// delete-capable or settings-mutating skill from running immediately off a message from this new,
/// physically-exposed channel. UserRights is the one lever that always applies regardless of autonomy
/// level.
/// </summary>
/// <param name="ownerMessengerReader">Resolves the owner's registered Slack self-alias.</param>
/// <param name="messagingService">Reads inbound Slack messages and sends the reply back out.</param>
/// <param name="settingsRepository">Persists the polling watermark (last processed message timestamp).</param>
/// <param name="planningAudienceResolver">Resolves the admin user id to run the LLM turn as.</param>
/// <param name="mediator">Dispatches the LLM chat turn through the same path the in-app chat uses.</param>
/// <param name="logger">Structured log per cycle and per failed message.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Plugin.Messaging.Application.Constants;
using Klacks.Plugin.Messaging.Application.Interfaces;
using Klacks.Plugin.Messaging.Domain.Enums;
using Klacks.Plugin.Messaging.Domain.Models;

namespace Klacks.Api.Application.Services.Assistant;

public class SlackOwnerBridgeService : ISlackOwnerBridgeService
{
    private const int MaxMessagesPerCycle = 50;
    private const string ConversationIdPrefix = "slack-owner:";

    private readonly IOwnerMessengerReader _ownerMessengerReader;
    private readonly IMessagingService _messagingService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanningAudienceResolver _planningAudienceResolver;
    private readonly IMediator _mediator;
    private readonly IInternalTokenIssuer _internalTokenIssuer;
    private readonly ILogger<SlackOwnerBridgeService> _logger;

    public SlackOwnerBridgeService(
        IOwnerMessengerReader ownerMessengerReader,
        IMessagingService messagingService,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        IPlanningAudienceResolver planningAudienceResolver,
        IMediator mediator,
        IInternalTokenIssuer internalTokenIssuer,
        ILogger<SlackOwnerBridgeService> logger)
    {
        _ownerMessengerReader = ownerMessengerReader;
        _messagingService = messagingService;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _planningAudienceResolver = planningAudienceResolver;
        _mediator = mediator;
        _internalTokenIssuer = internalTokenIssuer;
        _logger = logger;
    }

    public async Task<int> RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var ownerAlias = await _ownerMessengerReader.GetByTypeAsync(MessengerType.Slack, cancellationToken);
        if (ownerAlias == null || string.IsNullOrWhiteSpace(ownerAlias.Value))
        {
            return 0;
        }

        var watermark = await GetWatermarkAsync();
        if (watermark == null)
        {
            // First-ever activation: start from now so the entire past Slack history for this sender
            // is never replayed as if newly arrived.
            await PersistWatermarkAsync(DateTime.UtcNow);
            return 0;
        }

        var candidates = await _messagingService.GetMessagesAsync(
            providerId: null,
            direction: MessageDirection.Inbound,
            sender: ownerAlias.Value,
            count: MaxMessagesPerCycle,
            offset: 0,
            cancellationToken);

        var newMessages = candidates
            .Where(m => m.Timestamp > watermark.Value)
            .OrderBy(m => m.Timestamp)
            .ToList();

        if (newMessages.Count == 0)
        {
            return 0;
        }

        var executingAdminId = await ResolveExecutingAdminIdAsync(cancellationToken);
        if (executingAdminId == null)
        {
            _logger.LogWarning(
                "SlackOwnerBridgeService - no admin user resolvable, leaving {Count} message(s) for a later cycle",
                newMessages.Count);
            return 0;
        }

        var processed = 0;
        foreach (var message in newMessages)
        {
            try
            {
                await ReplyToMessageAsync(message, executingAdminId, cancellationToken);
                processed++;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(
                    ex, "SlackOwnerBridgeService - failed to process inbound Slack message {MessageId}", message.Id);
            }
        }

        // Advance past every attempted message, not just the successful ones — see class summary.
        await PersistWatermarkAsync(newMessages[^1].Timestamp);

        return processed;
    }

    private async Task ReplyToMessageAsync(Message message, string executingAdminId, CancellationToken ct)
    {
        // Capped at Authorised on purpose, unchanged from before: Slack is a physically exposed channel,
        // so even an Admin owner must not reach Admin-only actions through it. What changed is that the
        // turn now carries a real token, so a skill that mutates reaches the REST API under this
        // identity instead of writing past it.
        BearerToken? accessToken = null;
        var userRights = Permissions.GetPermissionsForRole(Roles.Authorised).ToList();
        if (Guid.TryParse(executingAdminId, out var adminUserId))
        {
            var token = await _internalTokenIssuer.IssueForOwnerAsync(adminUserId, Roles.Authorised, ct);
            if (token.Success)
            {
                accessToken = token.Token;
                userRights = Permissions.ExpandRoles(token.Roles);
            }
            else
            {
                _logger.LogWarning(
                    "Slack bridge answers without a token for admin {AdminId}: {Reason}; mutating skills " +
                    "will fail closed for this turn",
                    executingAdminId, token.Reason);
            }
        }

        var response = await _mediator.Send(
            new ProcessLLMMessageCommand
            {
                Message = message.Content,
                UserId = executingAdminId,
                ConversationId = ConversationIdPrefix + executingAdminId,
                UserRights = userRights,
                AccessToken = accessToken,
                Language = await ResolveLanguageAsync(),
            },
            ct);

        await _messagingService.SendMessageAsync(
            MessagingConstants.ProviderSlack,
            new SendMessageRequest(message.Sender, response.Message),
            ct);
    }

    /// <summary>
    /// There is no browser session behind an inbound message, so the UI language that the in-app
    /// chat passes along is not available. Falling back to the installation's configured default
    /// keeps the reply in the language the owner actually uses; without it the model answers in
    /// English regardless of what was written to it.
    /// </summary>
    private async Task<string?> ResolveLanguageAsync()
    {
        var setting = await _settingsRepository.GetSetting(SettingKeys.DefaultLanguage);
        return string.IsNullOrWhiteSpace(setting?.Value) ? null : setting.Value;
    }

    private async Task<string?> ResolveExecutingAdminIdAsync(CancellationToken ct)
    {
        var adminIds = (await _planningAudienceResolver.GetAdminUserIdsAsync(ct))
            .Where(id => Guid.TryParse(id, out _))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return adminIds.Count > 0 ? adminIds[0] : null;
    }

    private async Task<DateTime?> GetWatermarkAsync()
    {
        var setting = await _settingsRepository.GetSetting(Settings.SLACK_OWNER_BRIDGE_WATERMARK);
        if (setting == null)
        {
            return null;
        }

        return DateTime.TryParse(
            setting.Value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var watermark)
            ? watermark
            : null;
    }

    private async Task PersistWatermarkAsync(DateTime watermark)
    {
        // UpsertSettingAsync only stages the change on the DbContext - AddSetting/PutSetting do not
        // save. Without this commit the watermark never reaches the database, every cycle sees a null
        // watermark, takes the "first-ever activation" branch and returns without processing anything.
        await _settingsRepository.UpsertSettingAsync(
            Settings.SLACK_OWNER_BRIDGE_WATERMARK,
            watermark.ToString("o", CultureInfo.InvariantCulture));

        await _unitOfWork.CompleteAsync();
    }
}
