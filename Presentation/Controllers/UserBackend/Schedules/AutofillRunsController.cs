// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// Read-only presence of the autofill runs in progress, for the progress line every viewer of a group sees. Open to
/// every signed-in user; the handler filters by group visibility. Starting and cancelling a run stay on the
/// admin-only AutoWizardController.
/// </summary>
/// <param name="mediator">Dispatches the query</param>
public class AutofillRunsController : BaseController
{
    private readonly IMediator _mediator;

    public AutofillRunsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("Active")]
    public async Task<ActionResult<IReadOnlyList<AutofillRunPresenceDto>>> Active(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveAutofillRunsQuery(), cancellationToken);
        return Ok(result);
    }
}
