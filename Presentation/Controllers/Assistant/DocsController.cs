// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Docs;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/docs")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DocsController : ControllerBase
{
    private readonly ILogger<DocsController> _logger;

    public DocsController(ILogger<DocsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<object> GetAvailableDocs()
    {
        var docs = DocsReader.GetAvailableDocs().Select(d => new
        {
            Name = d.Key,
            Description = d.Value,
            Url = $"/api/backend/assistant/docs/{d.Key}"
        });

        return Ok(new { Documents = docs });
    }

    [HttpGet("{docName}")]
    public async Task<ActionResult<object>> GetDocument(string docName)
    {
        if (!DocsReader.DocExists(docName))
        {
            return NotFound(new { Error = $"Document '{docName}' not found", AvailableDocuments = DocsReader.GetAvailableDocs().Keys });
        }

        var content = await DocsReader.ReadDocAsync(docName);

        if (string.IsNullOrEmpty(content))
        {
            return NotFound(new { Error = $"Document '{docName}' could not be loaded" });
        }

        _logger.LogInformation("Serving documentation: {DocName}", docName);

        return Ok(new
        {
            Name = docName,
            Description = DocsReader.GetDocDescription(docName),
            Content = content,
            MimeType = "text/markdown"
        });
    }

    [HttpGet("{docName}/raw")]
    public async Task<ActionResult> GetDocumentRaw(string docName)
    {
        if (!DocsReader.DocExists(docName))
        {
            return NotFound($"Document '{docName}' not found");
        }

        var content = await DocsReader.ReadDocAsync(docName);

        if (string.IsNullOrEmpty(content))
        {
            return NotFound($"Document '{docName}' could not be loaded");
        }

        return Content(content, "text/markdown");
    }
}
