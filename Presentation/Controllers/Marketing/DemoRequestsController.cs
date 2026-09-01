// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Models.Marketing;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Marketing;

[ApiController]
[Route("api/[controller]")]
public class DemoRequestsController : ControllerBase
{
    private readonly DataBaseContext _db;

    public DemoRequestsController(DataBaseContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Submit a demo request from the marketing site contact form.
    /// Public endpoint — no authentication required.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(DemoRequestDto input, CancellationToken ct)
    {
        var entity = new DemoRequest
        {
            Name = input.Name.Trim(),
            Email = input.Email.Trim().ToLowerInvariant(),
            Company = input.Company?.Trim(),
            CountryCode = input.CountryCode?.Trim(),
            Industry = input.Industry?.Trim(),
            Message = input.Message?.Trim(),
            UtmCampaign = input.UtmCampaign?.Trim(),
            SourceUrl = input.SourceUrl?.Trim(),
            CreateTime = DateTime.UtcNow,
        };

        _db.DemoRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { Id = entity.Id });
    }

    public record DemoRequestDto
    {
        [Required, MaxLength(200)]
        public required string Name { get; init; }

        [Required, EmailAddress, MaxLength(200)]
        public required string Email { get; init; }

        [MaxLength(200)]
        public string? Company { get; init; }

        [MaxLength(10)]
        public string? CountryCode { get; init; }

        [MaxLength(50)]
        public string? Industry { get; init; }

        [MaxLength(2000)]
        public string? Message { get; init; }

        [MaxLength(200)]
        public string? UtmCampaign { get; init; }

        [MaxLength(500)]
        public string? SourceUrl { get; init; }
    }
}
