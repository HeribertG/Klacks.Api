// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Endpoints for retrieving and dismissing LLM model sync notifications.
/// </summary>
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/sync-notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ModelSyncController : ControllerBase
{
    private readonly ILLMRepository _repository;
    private readonly ILogger<ModelSyncController> _logger;

    public ModelSyncController(ILLMRepository repository, ILogger<ModelSyncController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetUnread()
    {
        var notifications = await _repository.GetUnreadSyncNotificationsAsync();

        var response = notifications.Select(n => new
        {
            id = n.Id.ToString(),
            providerId = n.ProviderId,
            providerName = n.ProviderName,
            newModelsCount = n.NewModelsCount,
            deactivatedModelsCount = n.DeactivatedModelsCount,
            newModelNames = n.NewModelNames,
            deactivatedModelNames = n.DeactivatedModelNames,
            syncedAt = n.SyncedAt,
        });

        return Ok(response);
    }

    [HttpPost("mark-read")]
    public async Task<ActionResult> MarkAllRead()
    {
        await _repository.MarkAllSyncNotificationsReadAsync();
        return NoContent();
    }
}
