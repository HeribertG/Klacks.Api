// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for CRUD operations on custom speech-to-text provider configurations.
/// </summary>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Presentation.DTOs.Assistant;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/stt/providers/custom")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CustomSttProviderController : ControllerBase
{
    private readonly ICustomSttProviderRepository _repository;
    private const string MaskedApiKey = "***";

    public CustomSttProviderController(ICustomSttProviderRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var providers = await _repository.GetAllAsync(ct);
        var dtos = providers.Select(p => new CustomSttProviderDto(
            p.Id, p.Name, p.ConnectionType, p.ApiUrl,
            string.IsNullOrWhiteSpace(p.ApiKey) ? null : MaskedApiKey,
            p.LanguageModel, p.IsEnabled, p.IsSystem)).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomSttProviderDto dto, CancellationToken ct)
    {
        var provider = new CustomSttProvider
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ConnectionType = dto.ConnectionType,
            ApiUrl = dto.ApiUrl,
            ApiKey = dto.ApiKey,
            LanguageModel = dto.LanguageModel,
            IsEnabled = dto.IsEnabled,
            IsSystem = false
        };

        await _repository.AddAsync(provider, ct);

        var result = new CustomSttProviderDto(
            provider.Id, provider.Name, provider.ConnectionType, provider.ApiUrl,
            MaskedApiKey, provider.LanguageModel, provider.IsEnabled, provider.IsSystem);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CustomSttProviderDto dto, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        existing.Name = dto.Name;
        existing.ConnectionType = dto.ConnectionType;
        existing.ApiUrl = dto.ApiUrl;
        existing.LanguageModel = dto.LanguageModel;
        existing.IsEnabled = dto.IsEnabled;

        if (dto.ApiKey != MaskedApiKey)
        {
            existing.ApiKey = dto.ApiKey;
        }

        await _repository.UpdateAsync(existing, ct);

        var result = new CustomSttProviderDto(
            existing.Id, existing.Name, existing.ConnectionType, existing.ApiUrl,
            string.IsNullOrWhiteSpace(existing.ApiKey) ? null : MaskedApiKey,
            existing.LanguageModel, existing.IsEnabled, existing.IsSystem);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        if (existing.IsSystem)
            return BadRequest("System providers cannot be deleted");

        await _repository.DeleteAsync(id, ct);
        return NoContent();
    }
}
