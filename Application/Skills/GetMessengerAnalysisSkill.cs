// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shows what the autonomous inbound-intelligence pipeline made of a received messenger message: the
/// detected intent, the summary, the resolved client, the extracted date/time window, the failure reason
/// when the analysis could not be acted on, and - when Klacksy asked the employee back about it - the
/// clarification question and its state. Messages the pipeline has not analyzed yet report exactly that, and
/// so do messages attributed to a client the caller may not see - a hidden client is answered like a missing one.
/// A message the pipeline could not attribute to any client is shown to admins only, as the messaging plugin
/// does with its unattributed messages: nothing proves it does not come from a hidden employee.
/// </summary>
/// <param name="messageId">Required. UUID of the messenger message.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Inbound;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_messenger_analysis")]
public class GetMessengerAnalysisSkill : BaseSkillImplementation
{
    private readonly IInboundAnalysisRepository _analysisRepository;
    private readonly IInboundClarificationRepository _clarificationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public GetMessengerAnalysisSkill(
        IInboundAnalysisRepository analysisRepository,
        IInboundClarificationRepository clarificationRepository,
        IClientVisibilityGuard clientVisibilityGuard)
    {
        _analysisRepository = analysisRepository;
        _clarificationRepository = clarificationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var messageId = GetRequiredGuid(parameters, "messageId");

        var analysis = await _analysisRepository.GetBySourceAsync(InboundSourceKind.Messenger, messageId, cancellationToken);
        if (analysis == null || !await IsVisibleToCallerAsync(context, analysis, cancellationToken))
        {
            return SkillResult.SuccessResult(
                new { MessageId = messageId, Analyzed = false },
                "This message has not been analyzed by the inbound-intelligence pipeline (yet).");
        }

        var clarification = await _clarificationRepository.GetByAnalysisIdAsync(analysis.Id, cancellationToken);
        var outcome = string.IsNullOrWhiteSpace(analysis.FailureReason)
            ? "The analysis completed without a failure reason."
            : $"The analysis could not be acted on: {analysis.FailureReason}";

        return SkillResult.SuccessResult(
            new
            {
                MessageId = messageId,
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
                analysis.FailureReason,
                Clarification = clarification == null ? null : ClarificationStatusText.ToSkillData(clarification)
            },
            $"Message was analyzed on {analysis.AnalyzedAt:yyyy-MM-dd HH:mm} UTC: " +
            $"intent {analysis.Intent}, summary: {analysis.Summary} {outcome}" +
            (clarification == null ? string.Empty : ClarificationStatusText.Sentence(clarification)));
    }

    private async Task<bool> IsVisibleToCallerAsync(
        SkillExecutionContext context, InboundAnalysis analysis, CancellationToken cancellationToken)
    {
        if (analysis.ClientId is { } clientId)
        {
            return await _clientVisibilityGuard.IsVisibleAsync(clientId, cancellationToken);
        }

        return context.UserPermissions.Contains(Roles.Admin);
    }
}
