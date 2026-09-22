// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows what the autonomous inbound-intelligence pipeline made of a received messenger message: the
/// detected intent, the summary, the resolved client, the extracted date/time window and — when the
/// analysis could not be acted on — the failure reason. Messages the pipeline has not analyzed yet
/// report exactly that.
/// </summary>
/// <param name="messageId">Required. UUID of the messenger message.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_messenger_analysis")]
public class GetMessengerAnalysisSkill : BaseSkillImplementation
{
    private readonly IInboundAnalysisRepository _analysisRepository;

    public GetMessengerAnalysisSkill(IInboundAnalysisRepository analysisRepository)
    {
        _analysisRepository = analysisRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var messageId = GetRequiredGuid(parameters, "messageId");

        var analysis = await _analysisRepository.GetBySourceAsync(InboundSourceKind.Messenger, messageId, cancellationToken);
        if (analysis == null)
        {
            return SkillResult.SuccessResult(
                new { MessageId = messageId, Analyzed = false },
                "This message has not been analyzed by the inbound-intelligence pipeline (yet).");
        }

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
                analysis.FailureReason
            },
            $"Message was analyzed on {analysis.AnalyzedAt:yyyy-MM-dd HH:mm} UTC: " +
            $"intent {analysis.Intent}, summary: {analysis.Summary} {outcome}");
    }
}
