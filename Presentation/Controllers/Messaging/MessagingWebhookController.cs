// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Webhook endpoint for receiving incoming messages from external messaging providers.
/// Handles webhook verification and incoming message processing.
/// </summary>
using Klacks.Api.Application.DTOs.Messaging;
using Klacks.Api.Application.Interfaces.Messaging;
using Klacks.Api.Domain.Interfaces.Messaging;
using Klacks.Api.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Presentation.Controllers.Messaging;

[ApiController]
[Route("api/messaging/webhook")]
[AllowAnonymous]
public class MessagingWebhookController : ControllerBase
{
    private readonly IMessagingService _messagingService;
    private readonly IMessagingProviderRepository _providerRepository;
    private readonly IHubContext<AssistantNotificationHub, IAssistantClient> _hubContext;
    private readonly IAssistantConnectionTracker _tracker;
    private readonly ILogger<MessagingWebhookController> _logger;

    public MessagingWebhookController(
        IMessagingService messagingService,
        IMessagingProviderRepository providerRepository,
        IHubContext<AssistantNotificationHub, IAssistantClient> hubContext,
        IAssistantConnectionTracker tracker,
        ILogger<MessagingWebhookController> logger)
    {
        _messagingService = messagingService;
        _providerRepository = providerRepository;
        _hubContext = hubContext;
        _tracker = tracker;
        _logger = logger;
    }

    [HttpPost("{providerName}")]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> ReceiveWebhook(string providerName)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault()
            ?? Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();

        try
        {
            var message = await _messagingService.ProcessIncomingMessageAsync(providerName, body, signature);
            var provider = await _providerRepository.GetByNameAsync(providerName);

            var notification = new IncomingMessageDto
            {
                MessageId = message.Id,
                ProviderName = provider?.Name ?? providerName,
                ProviderDisplayName = provider?.DisplayName ?? providerName,
                Sender = message.Sender,
                SenderDisplayName = message.SenderDisplayName,
                Content = message.Content,
                ContentType = message.ContentType,
                Timestamp = message.Timestamp
            };

            await BroadcastIncomingMessageAsync(notification);

            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook request for provider {Provider}", providerName);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("{providerName}")]
    public IActionResult VerifyWebhook(string providerName, [FromQuery(Name = "hub.challenge")] string? challenge = null)
    {
        if (!string.IsNullOrEmpty(challenge) && challenge.Length <= 256)
            return Ok(challenge);

        return Ok("Webhook endpoint active");
    }

    private async Task BroadcastIncomingMessageAsync(IncomingMessageDto notification)
    {
        var connectedUserIds = _tracker.GetConnectedUserIds().ToList();
        if (connectedUserIds.Count == 0) return;

        foreach (var userId in connectedUserIds)
        {
            var connectionIds = _tracker.GetConnectionIds(userId).ToList();
            if (connectionIds.Count > 0)
            {
                await _hubContext.Clients.Clients(connectionIds).IncomingMessage(notification);
            }
        }
    }
}
