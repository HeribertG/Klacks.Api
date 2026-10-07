// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stages replacement request book rows for cover_absence proposals and keeps the ManualReplacement row of a
/// replacement WorkChange in step with that change (create, update, discard). Times are raw facts: the report
/// instant (never in the future and never more than ReplacementRequestLimits.MaxReportLeadDays before the first
/// covered slot) and the slot start converted from the company time zone. A manual ReplacementStart/End stores
/// no own times, so its window comes from <see cref="ReplacementWindow"/> (hours clamped to the work's length,
/// placed on the calendar - after midnight of a night shift means the next day). Lives in Infrastructure because
/// that window calculation does.
/// </summary>
/// <param name="repository">Stage-only request book repository</param>
/// <param name="shiftRepository">Maps a scenario clone shift back to its source shift</param>
/// <param name="phoneResolver">Visible candidates' phone numbers</param>
/// <param name="companyClock">Company time zone for the slot start</param>
/// <param name="timeProvider">Current instant</param>
/// <param name="httpContextAccessor">User a manual replacement is recorded under</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.Recovery;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ReplacementRequestRecorder : IReplacementRequestRecorder
{
    private readonly IReplacementRequestRepository _repository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IReplacementContactPhoneResolver _phoneResolver;
    private readonly ICompanyClock _companyClock;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReplacementRequestRecorder(
        IReplacementRequestRepository repository,
        IShiftRepository shiftRepository,
        IReplacementContactPhoneResolver phoneResolver,
        ICompanyClock companyClock,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _shiftRepository = shiftRepository;
        _phoneResolver = phoneResolver;
        _companyClock = companyClock;
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<CoveredSlot>> RecordProposalsAsync(
        ReplacementProposalContext context, IReadOnlyList<CoveredSlot> covered, CancellationToken cancellationToken = default)
    {
        var recordable = covered.Where(slot => slot.ShiftId != Guid.Empty).ToList();
        if (recordable.Count == 0)
        {
            return covered;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var timeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var reportedAtUtc = BoundReportedAt(context.ReportedAtUtc, nowUtc, recordable.Min(slot => slot.Date), timeZone);
        var phones = await _phoneResolver.ResolveAsync(
            recordable.Select(slot => slot.ReplacementClientId).ToList(), cancellationToken);

        var existing = (await _repository.ListLiveByTokenAsync(context.AnalyseToken, cancellationToken))
            .GroupBy(r => (r.CandidateClientId, r.ShiftId, r.Date))
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<CoveredSlot>(covered.Count);
        foreach (var slot in covered)
        {
            if (slot.ShiftId == Guid.Empty)
            {
                result.Add(slot);
                continue;
            }

            var key = (slot.ReplacementClientId, slot.ShiftId, slot.Date);
            if (!existing.TryGetValue(key, out var row))
            {
                var start = slot.StartTime ?? TimeOnly.MinValue;
                row = new ReplacementRequest
                {
                    AbsentClientId = context.AbsentClientId,
                    CandidateClientId = slot.ReplacementClientId,
                    ShiftId = slot.ShiftId,
                    Date = slot.Date,
                    StartTime = start,
                    EndTime = slot.EndTime ?? start,
                    GroupId = context.GroupId,
                    AbsenceId = context.AbsenceId,
                    Source = context.Source,
                    Outcome = ReplacementRequestOutcome.Proposed,
                    ReportedAtUtc = reportedAtUtc,
                    ShiftStartUtc = WallClockToUtc(slot.Date.ToDateTime(start), timeZone),
                    AnalyseToken = context.AnalyseToken
                };
                await _repository.Add(row);
                existing[key] = row;
            }

            result.Add(slot with
            {
                RequestId = row.Id,
                Outcome = row.Outcome,
                Phone = phones.TryGetValue(slot.ReplacementClientId, out var phone) ? phone : null
            });
        }

        return result;
    }

    public async Task RecordManualReplacementAsync(Work parentWork, WorkChange workChange, CancellationToken cancellationToken = default)
    {
        if (!IsManualReplacement(workChange, out var candidateId))
        {
            return;
        }

        var slot = await ResolveSlotAsync(parentWork, workChange, cancellationToken);
        var existing = await _repository.FindLiveAsync(
            parentWork.AnalyseToken, candidateId, slot.ShiftId, parentWork.CurrentDate, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _repository.Add(new ReplacementRequest
        {
            AbsentClientId = parentWork.ClientId,
            CandidateClientId = candidateId,
            ShiftId = slot.ShiftId,
            Date = parentWork.CurrentDate,
            StartTime = slot.Start,
            EndTime = slot.End,
            Source = ReplacementRequestSource.ManualReplacement,
            Outcome = ReplacementRequestOutcome.Accepted,
            ReportedAtUtc = nowUtc,
            ShiftStartUtc = slot.StartUtc,
            OutcomeAtUtc = nowUtc,
            OutcomeByUserId = ReplacementRequestActor.CurrentUserId(_httpContextAccessor),
            AnalyseToken = parentWork.AnalyseToken,
            WorkChangeId = workChange.Id == Guid.Empty ? null : workChange.Id,
            AppliedAtUtc = parentWork.AnalyseToken is null ? nowUtc : null
        });
    }

    public async Task SyncManualReplacementAsync(Work parentWork, WorkChange workChange, CancellationToken cancellationToken = default)
    {
        var row = await _repository.FindManualByWorkChangeIdAsync(workChange.Id, cancellationToken);
        if (row is null)
        {
            await RecordManualReplacementAsync(parentWork, workChange, cancellationToken);
            return;
        }

        if (!IsManualReplacement(workChange, out var candidateId))
        {
            _repository.Remove(row);
            return;
        }

        var slot = await ResolveSlotAsync(parentWork, workChange, cancellationToken);
        var keyOwner = await _repository.FindLiveAsync(
            parentWork.AnalyseToken, candidateId, slot.ShiftId, parentWork.CurrentDate, cancellationToken);
        if (keyOwner is not null && keyOwner.Id != row.Id)
        {
            _repository.Remove(row);
            return;
        }

        row.AbsentClientId = parentWork.ClientId;
        row.CandidateClientId = candidateId;
        row.ShiftId = slot.ShiftId;
        row.Date = parentWork.CurrentDate;
        row.StartTime = slot.Start;
        row.EndTime = slot.End;
        row.ShiftStartUtc = slot.StartUtc;
    }

    public async Task DiscardManualReplacementAsync(Guid workChangeId, CancellationToken cancellationToken = default)
    {
        var row = await _repository.FindManualByWorkChangeIdAsync(workChangeId, cancellationToken);
        if (row is not null)
        {
            _repository.Remove(row);
        }
    }

    private static bool IsManualReplacement(WorkChange workChange, out Guid candidateId)
    {
        candidateId = workChange.ReplaceClientId ?? Guid.Empty;
        return ReplacementWorkChangeTypes.IsReplacement(workChange.Type)
            && workChange.ReplaceClientId.HasValue
            && !string.Equals(workChange.Description, RecoveryMarkers.WorkChangeSource, StringComparison.Ordinal);
    }

    private async Task<(Guid ShiftId, TimeOnly Start, TimeOnly End, DateTime StartUtc)> ResolveSlotAsync(
        Work parentWork, WorkChange workChange, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetNoTracking(parentWork.ShiftId);
        var shiftId = shift?.ScenarioSourceShiftId ?? parentWork.ShiftId;
        var hours = ReplacementWindow.ClampHours(parentWork.StartTime, parentWork.EndTime, workChange.ChangeTime);
        var (start, end) = ReplacementWindow.Compute(
            workChange.Type, parentWork.StartTime, parentWork.EndTime, workChange.StartTime, workChange.EndTime, hours);
        var (startAt, _) = ReplacementWindow.ToInterval(parentWork.CurrentDate, parentWork.StartTime, start, end);
        var timeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);

        return (shiftId, start, end, WallClockToUtc(startAt, timeZone));
    }

    private static DateTime BoundReportedAt(DateTime? reportedAtUtc, DateTime nowUtc, DateOnly firstSlotDate, TimeZoneInfo timeZone)
    {
        if (reportedAtUtc is not { } reported)
        {
            return nowUtc;
        }

        var utc = reported.Kind switch
        {
            DateTimeKind.Utc => reported,
            DateTimeKind.Local => reported.ToUniversalTime(),
            _ => DateTime.SpecifyKind(reported, DateTimeKind.Utc)
        };

        var leadBound = WallClockToUtc(
            firstSlotDate.AddDays(-ReplacementRequestLimits.MaxReportLeadDays).ToDateTime(TimeOnly.MinValue), timeZone);
        var earliest = leadBound < nowUtc ? leadBound : nowUtc;

        if (utc > nowUtc)
        {
            return nowUtc;
        }

        return utc < earliest ? earliest : utc;
    }

    private static DateTime WallClockToUtc(DateTime wallClock, TimeZoneInfo timeZone)
        => CompanyWallClockToUtcConverter.ConvertToUtc(wallClock, timeZone);
}
