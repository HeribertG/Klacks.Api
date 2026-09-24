// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A planner takes over an open follow-up question Klacksy sent to an employee: the clarification moves
/// from open to taken over (a conditional transition, so an answer or the expiry sweep arriving in the
/// same moment wins or loses cleanly), the change is verified by re-reading it, and later messages of
/// the employee are analyzed normally again. After a lost race the clarification is re-read so the error
/// names what really happened. The clarification is addressed by its id or by the employee whose open
/// clarification it is. The employee is named from the client record, never from the sender text of the
/// inbound message, because an outside sender controls that text and this skill's output is not treated
/// as untrusted.
/// </summary>
/// <param name="clarificationId">Optional. UUID of the open clarification.</param>
/// <param name="clientId">Optional. UUID of the employee whose open clarification is closed; when given together with the clarification id it must be the employee the clarification belongs to.</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Inbound;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class CloseClarificationSkill : BaseSkillImplementation
{
    private const string SkillName = "close_clarification";
    private const string ClarificationIdParameter = "clarificationId";
    private const string ClientIdParameter = "clientId";
    private const string MissingTargetError = "Name the open follow-up question or the employee it was sent to.";
    private const string NotFoundError = "There is no open follow-up question for this employee.";
    private const string NotFoundByIdError = "No follow-up question with this id exists.";
    private const string ClientMismatchError = "This follow-up question does not belong to that employee.";
    private const string FallbackEmployeeLabel = "the employee";
    private const string NeverSentError =
        "This follow-up question was only suggested to the planners and never sent, so there is nothing to close.";
    private const string AlreadyClosedErrorFormat = "This follow-up question is already closed: {0}.";
    private const string ClosedMeanwhileError = "The follow-up question was closed in the meantime.";
    private const string ClosedMeanwhileErrorFormat = "The follow-up question was closed in the meantime: {0}.";
    private const string VerificationFailedFormat =
        "Database verification failed: follow-up question {0} could not be confirmed as taken over after the update. " +
        "Check its current state before trying again.";
    private const string SuccessMessageFormat =
        "The open follow-up question to {0} is closed and taken over by you (verified). Klacksy no longer waits " +
        "for an answer; later messages from this employee are evaluated normally again.";

    private readonly IInboundClarificationRepository _clarificationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly TimeProvider _timeProvider;

    public CloseClarificationSkill(
        IInboundClarificationRepository clarificationRepository,
        IClientRepository clientRepository,
        TimeProvider timeProvider)
    {
        _clarificationRepository = clarificationRepository;
        _clientRepository = clientRepository;
        _timeProvider = timeProvider;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clarificationId = GetParameter<Guid?>(parameters, ClarificationIdParameter);
        var clientId = GetParameter<Guid?>(parameters, ClientIdParameter);

        InboundClarification? clarification;
        string notFoundError;
        if (clarificationId is { } id)
        {
            clarification = await _clarificationRepository.GetByIdAsync(id, cancellationToken);
            notFoundError = NotFoundByIdError;
        }
        else if (clientId is { } client)
        {
            clarification = await _clarificationRepository.GetOpenByClientAsync(client, cancellationToken);
            notFoundError = NotFoundError;
        }
        else
        {
            return SkillResult.Error(MissingTargetError);
        }

        if (clarification == null)
        {
            return SkillResult.Error(notFoundError);
        }

        if (clarificationId != null && clientId is { } expectedClient && clarification.ClientId != expectedClient)
        {
            return SkillResult.Error(ClientMismatchError);
        }

        if (clarification.Status == InboundClarificationStatus.Suggested)
        {
            return SkillResult.Error(NeverSentError);
        }

        if (clarification.Status != InboundClarificationStatus.Open)
        {
            return SkillResult.Error(string.Format(
                CultureInfo.InvariantCulture, AlreadyClosedErrorFormat, ClarificationStatusText.Describe(clarification)));
        }

        var closed = await _clarificationRepository.TryResolveAsync(
            clarification.Id,
            InboundClarificationStatus.TakenOver,
            answerSourceId: null,
            resultAnalysisId: null,
            resolvedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken: cancellationToken);
        if (!closed)
        {
            return SkillResult.Error(await DescribeLostRaceAsync(clarification.Id, cancellationToken));
        }

        var reread = await _clarificationRepository.GetByIdAsync(clarification.Id, cancellationToken);
        if (reread is not { Status: InboundClarificationStatus.TakenOver })
        {
            throw new SkillVerificationException(
                SkillName, string.Format(CultureInfo.InvariantCulture, VerificationFailedFormat, clarification.Id));
        }

        var employeeLabel = await ResolveEmployeeLabelAsync(clarification.ClientId, cancellationToken);

        return SkillResult.SuccessResult(
            new
            {
                ClarificationId = clarification.Id,
                clarification.ClientId,
                Status = ClarificationStatusText.Describe(InboundClarificationStatus.TakenOver)
            },
            string.Format(CultureInfo.InvariantCulture, SuccessMessageFormat, employeeLabel));
    }

    private async Task<string> ResolveEmployeeLabelAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetTypeAndDisplayNameAsync(clientId, cancellationToken);
        return string.IsNullOrWhiteSpace(client?.DisplayName) ? FallbackEmployeeLabel : client.DisplayName;
    }

    private async Task<string> DescribeLostRaceAsync(Guid clarificationId, CancellationToken cancellationToken)
    {
        var current = await _clarificationRepository.GetByIdAsync(clarificationId, cancellationToken);
        if (current == null || current.Status is InboundClarificationStatus.Open or InboundClarificationStatus.Suggested)
        {
            return ClosedMeanwhileError;
        }

        return string.Format(
            CultureInfo.InvariantCulture, ClosedMeanwhileErrorFormat, ClarificationStatusText.Describe(current));
    }
}
