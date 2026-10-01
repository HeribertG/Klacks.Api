// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Computes the preview of an employee import: every row as it would be written, with status and
/// findings. Read-only; it runs the same evaluator the commit runs.
/// </summary>
/// <param name="evaluator">Shared evaluation path of preview and commit</param>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Application.Queries.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientImport;

public class PreviewClientImportQueryHandler : BaseHandler, IRequestHandler<PreviewClientImportQuery, ClientImportPreviewResult>
{
    private readonly ClientImportEvaluator _evaluator;

    public PreviewClientImportQueryHandler(ClientImportEvaluator evaluator, ILogger<PreviewClientImportQueryHandler> logger)
        : base(logger)
    {
        _evaluator = evaluator;
    }

    public async Task<ClientImportPreviewResult> Handle(PreviewClientImportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var drafts = await _evaluator.EvaluateAsync(request.Request, cancellationToken);
            return ClientImportPreviewBuilder.Build(request.Request, drafts);
        },
        "previewing client import",
        new { RowCount = request.Request.Rows?.Count });
    }
}
