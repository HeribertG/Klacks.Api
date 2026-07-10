// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Triggers an immediate IMAP fetch of new emails (the inbox "refresh now" button) instead
/// of waiting for the next background polling cycle, and reports how many new emails arrived.
/// </summary>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("fetch_new_emails")]
public class FetchNewEmailsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public FetchNewEmailsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new FetchEmailsCommand(), cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(
                $"Fetching emails from the IMAP server failed: {result.Error ?? "unknown error"}. " +
                "Check the IMAP settings with get_imap_settings / test_imap_connection.");
        }

        var message = result.FetchedCount == 0
            ? "The mailbox was checked — no new emails."
            : $"{result.FetchedCount} new email(s) were fetched. Use list_emails to see them.";

        return SkillResult.SuccessResult(
            new { result.FetchedCount },
            message);
    }
}
