// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows what the autonomous email-intelligence pipeline made of a received email: the
/// detected intent (absence, availability, shift preference …), the summary, the resolved
/// employee, the extracted date/time window and — when the analysis could not be acted on —
/// the failure reason. Emails the pipeline has not analyzed yet report exactly that.
/// </summary>
/// <param name="emailId">Required. UUID of the received email (from list_emails).</param>

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_email_analysis")]
public class GetEmailAnalysisSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IEmailAnalysisRepository _analysisRepository;

    public GetEmailAnalysisSkill(IMediator mediator, IEmailAnalysisRepository analysisRepository)
    {
        _mediator = mediator;
        _analysisRepository = analysisRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var emailId = GetRequiredGuid(parameters, "emailId");

        var email = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (email == null)
        {
            return SkillResult.Error($"Email '{emailId}' not found.");
        }

        var analysis = await _analysisRepository.GetByReceivedEmailIdAsync(emailId, cancellationToken);
        if (analysis == null)
        {
            return SkillResult.SuccessResult(
                new { EmailId = emailId, email.Subject, Analyzed = false },
                $"Email '{email.Subject}' has not been analyzed by the email-intelligence pipeline (yet). " +
                "The pipeline only analyzes newly fetched emails in the background.");
        }

        var outcome = string.IsNullOrWhiteSpace(analysis.FailureReason)
            ? "The analysis completed without a failure reason."
            : $"The analysis could not be acted on: {analysis.FailureReason}";

        return SkillResult.SuccessResult(
            new
            {
                EmailId = emailId,
                email.Subject,
                Analyzed = true,
                Intent = analysis.Intent.ToString(),
                analysis.Summary,
                analysis.ClientId,
                ClientType = analysis.ClientType?.ToString(),
                analysis.FromDate,
                analysis.UntilDate,
                analysis.StartHour,
                analysis.EndHour,
                analysis.Weekdays,
                analysis.ScheduleCommands,
                analysis.AnalyzedAt,
                analysis.FailureReason
            },
            $"Email '{email.Subject}' was analyzed on {analysis.AnalyzedAt:yyyy-MM-dd HH:mm} UTC: " +
            $"intent {analysis.Intent}, summary: {analysis.Summary} {outcome}");
    }
}
