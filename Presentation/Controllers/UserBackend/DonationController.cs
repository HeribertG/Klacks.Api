// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Donations;
using Klacks.Api.Application.DTOs.Donations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

/// <summary>
/// Endpoints for the donation feature (Stripe checkout). A rejected request reports the same text under
/// both "message" and "detail": the browser dialog reads "message", while KlacksSelfApiClient — the path
/// the create_donation_checkout skill takes — only unpacks "errors", "detail" or "title" from a failure
/// body and would otherwise replace the concrete cause (Stripe not configured, unsupported currency)
/// with a generic "The request was rejected as invalid.".
/// </summary>
[ApiController]
public class DonationController : BaseController
{
    private const string UnavailableMessage = "Donation checkout is not available.";

    private readonly IMediator mediator;

    public DonationController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpPost("checkout-session")]
    public async Task<ActionResult<CreateDonationCheckoutResponse>> CreateCheckoutSession(
        [FromBody] CreateDonationCheckoutRequest request)
    {
        var response = await this.mediator.Send(new CreateDonationCheckoutCommand(request));

        if (response.Url == null)
        {
            var failure = response.ErrorMessage ?? UnavailableMessage;

            return BadRequest(new { message = failure, detail = failure });
        }

        return Ok(response);
    }
}
