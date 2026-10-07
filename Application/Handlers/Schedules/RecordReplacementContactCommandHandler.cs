// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="RecordReplacementContactCommand"/>: the planner phoned an alternative candidate from
/// the recovery dialog. After <see cref="IReplacementContactValidator"/> (every referenced entity exists and is
/// visible, hidden = not found) it upserts the row for (scenario token, candidate, shift, date): a new row is a
/// PlannerDialog row whose report time is the one already recorded for this absence in the scenario (else now),
/// an existing row only gets the new answer - unless it belongs to another absent employee, which is answered
/// like a missing employee. When a parallel request inserted the same natural key first, the unique index
/// refuses the insert and the handler re-reads the winner and records the answer on it. It never touches the
/// proposal or the scenario's WorkChanges (swapping the replacement is a separate step).
/// </summary>
/// <param name="repository">Stage-only request book repository</param>
/// <param name="validator">Existence, visibility and plausibility of the request</param>
/// <param name="unitOfWork">Commits the row</param>
/// <param name="settingsReader">Short-notice threshold for the returned resource</param>
/// <param name="companyClock">Company time zone for the slot start</param>
/// <param name="timeProvider">Answer instant</param>
/// <param name="httpContextAccessor">User the answer is recorded under</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Services.Schedules.Recovery;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class RecordReplacementContactCommandHandler
    : IRequestHandler<RecordReplacementContactCommand, ReplacementRequestResource>
{
    private const string ClientNotFoundMessage = "Client not found";
    private const string AlreadyAppliedMessage = "This replacement was already applied to the real plan; its answer can no longer change.";
    private const string NotAnAnswerMessage = "Outcome {0} cannot be recorded; use Requested, Accepted, Declined or NotReached.";

    private readonly IReplacementRequestRepository _repository;
    private readonly IReplacementContactValidator _validator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RecordReplacementContactCommandHandler(
        IReplacementRequestRepository repository,
        IReplacementContactValidator validator,
        IUnitOfWork unitOfWork,
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _validator = validator;
        _unitOfWork = unitOfWork;
        _settingsReader = settingsReader;
        _companyClock = companyClock;
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ReplacementRequestResource> Handle(
        RecordReplacementContactCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (!ReplacementRequestAnswers.IsAnswer(request.Outcome))
        {
            throw new InvalidRequestException(string.Format(NotAnAnswerMessage, request.Outcome));
        }

        await _validator.ValidateAsync(request, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var row = await FindOwnedRowAsync(request, cancellationToken);
        if (row?.AppliedAtUtc is not null)
        {
            throw new InvalidRequestException(AlreadyAppliedMessage);
        }

        if (row is null)
        {
            row = await NewRowAsync(request, nowUtc, cancellationToken);
            await _repository.Add(row);
        }

        Answer(row, request.Outcome, nowUtc);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DatabaseUpdateException ex) when (ex.IsDuplicate)
        {
            _repository.Detach(row);
            row = await FindOwnedRowAsync(request, cancellationToken);
            if (row is null)
            {
                throw;
            }

            Answer(row, request.Outcome, nowUtc);
            await _unitOfWork.CompleteAsync();
        }

        var shortNoticeHours = await ReplacementRequestSettingsReader.ReadShortNoticeHoursAsync(_settingsReader);
        return ReplacementRequestResourceMapper.ToResource(row, shortNoticeHours);
    }

    private async Task<ReplacementRequest?> FindOwnedRowAsync(
        RecordReplacementContactRequest request, CancellationToken cancellationToken)
    {
        var row = await _repository.FindLiveAsync(
            request.AnalyseToken, request.CandidateClientId, request.ShiftId, request.Date, cancellationToken);
        if (row is not null && row.AbsentClientId != request.AbsentClientId)
        {
            throw new KeyNotFoundException(ClientNotFoundMessage);
        }

        return row;
    }

    private async Task<ReplacementRequest> NewRowAsync(
        RecordReplacementContactRequest request, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var reportedAtUtc = request.AnalyseToken is { } token
            ? await _repository.FindReportedAtAsync(token, request.AbsentClientId, cancellationToken) ?? nowUtc
            : nowUtc;
        var timeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);

        return new ReplacementRequest
        {
            AbsentClientId = request.AbsentClientId,
            CandidateClientId = request.CandidateClientId,
            ShiftId = request.ShiftId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            GroupId = request.GroupId,
            AbsenceId = request.AbsenceId,
            Source = ReplacementRequestSource.PlannerDialog,
            ReportedAtUtc = reportedAtUtc,
            ShiftStartUtc = CompanyWallClockToUtcConverter.ConvertToUtc(
                request.Date.ToDateTime(request.StartTime), timeZone),
            AnalyseToken = request.AnalyseToken
        };
    }

    private void Answer(ReplacementRequest row, ReplacementRequestOutcome outcome, DateTime nowUtc)
    {
        row.Outcome = outcome;
        row.OutcomeAtUtc = nowUtc;
        row.OutcomeByUserId = ReplacementRequestActor.CurrentUserId(_httpContextAccessor);
    }
}
