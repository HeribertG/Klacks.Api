// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for CRUD operations on the transcription dictionary.
/// </summary>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Presentation.DTOs.Assistant;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/transcription/dictionary")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TranscriptionDictionaryController : ControllerBase
{
    private readonly ITranscriptionDictionaryRepository _repository;
    private readonly IDictionaryService _dictionaryService;

    public TranscriptionDictionaryController(ITranscriptionDictionaryRepository repository, IDictionaryService dictionaryService)
    {
        _repository = repository;
        _dictionaryService = dictionaryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var entries = await _repository.GetAllAsync(ct);
        var dtos = entries.Select(e => new TranscriptionDictionaryDto(
            e.Id, e.CorrectTerm, e.Category, e.PhoneticVariants, e.Description)).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TranscriptionDictionaryDto dto, CancellationToken ct)
    {
        var entry = new TranscriptionDictionaryEntry
        {
            Id = Guid.NewGuid(),
            CorrectTerm = dto.CorrectTerm,
            Category = dto.Category,
            PhoneticVariants = dto.PhoneticVariants,
            Description = dto.Description
        };

        await _repository.AddAsync(entry, ct);
        _dictionaryService.InvalidateCache();

        var result = new TranscriptionDictionaryDto(
            entry.Id, entry.CorrectTerm, entry.Category, entry.PhoneticVariants, entry.Description);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TranscriptionDictionaryDto dto, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        existing.CorrectTerm = dto.CorrectTerm;
        existing.Category = dto.Category;
        existing.PhoneticVariants = dto.PhoneticVariants;
        existing.Description = dto.Description;

        await _repository.UpdateAsync(existing, ct);
        _dictionaryService.InvalidateCache();

        var result = new TranscriptionDictionaryDto(
            existing.Id, existing.CorrectTerm, existing.Category, existing.PhoneticVariants, existing.Description);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        await _repository.DeleteAsync(id, ct);
        _dictionaryService.InvalidateCache();

        return NoContent();
    }
}
