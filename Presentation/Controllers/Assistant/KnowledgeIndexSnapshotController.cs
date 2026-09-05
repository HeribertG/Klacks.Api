// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

/// <summary>
/// Admin endpoint that exports the current knowledge_index embeddings as a downloadable snapshot
/// file, so it can be committed and used to seed a fresh database without re-embedding every entry.
/// </summary>
/// <param name="exporter">Builds the snapshot document from the current knowledge index content.</param>
[ApiController]
[Route("api/backend/knowledge-index")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class KnowledgeIndexSnapshotController : ControllerBase
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    private readonly IKnowledgeEmbeddingSnapshotExporter _exporter;

    public KnowledgeIndexSnapshotController(IKnowledgeEmbeddingSnapshotExporter exporter)
    {
        _exporter = exporter;
    }

    [HttpGet("snapshot")]
    public async Task<IActionResult> GetSnapshot(CancellationToken ct)
    {
        KnowledgeEmbeddingSnapshotDocument document;
        try
        {
            document = await _exporter.ExportAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document, SnapshotJsonOptions));
        return File(bytes, "application/json", KnowledgeIndexConstants.SnapshotFileName);
    }
}
