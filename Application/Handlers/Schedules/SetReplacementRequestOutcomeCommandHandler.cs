// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="SetReplacementRequestOutcomeCommand"/>: records who answered how and when on an
/// existing replacement request row. Open to every authenticated user (owner decision 2026-10-07), but group
/// visibility is a security boundary on BOTH people of the row: a missing row and a row whose candidate or
/// absent employee the caller may not see are answered with the same not found, never with forbidden. A row
/// whose replacement already reached the real plan (AppliedAtUtc set) is a closed fact and refuses a new answer.
/// </summary>
/// <param name="repository">Stage-only request book repository</param>
/// <param name="clientVisibilityGuard">Decides whether the caller may see both employees</param>
/// <param name="unitOfWork">Commits the change</param>
/// <param name="settingsReader">Short-notice threshold for the returned resource</param>
/// <param name="timeProvider">Answer instant</param>
/// <param name="httpContextAccessor">User the answer is recorded under</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Services.Schedules.Recovery;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class SetReplacementRequestOutcomeCommandHandler
    : IRequestHandler<SetReplacementRequestOutcomeCommand, ReplacementRequestResource>
{
    private const string NotFoundMessage = "Replacement request with ID {0} not found";
    private const string NotAnAnswerMessage = "Outcome {0} cannot be recorded; use Requested, Accepted, Declined or NotReached.";
    private const string AlreadyAppliedMessage = "Replacement request {0} was already applied to the real plan; its answer can no longer change.";

    private readonly IReplacementRequestRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingsReader _settingsReader;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SetReplacementRequestOutcomeCommandHandler(
        IReplacementRequestRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        ISettingsReader settingsReader,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
        _settingsReader = settingsReader;
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ReplacementRequestResource> Handle(
        SetReplacementRequestOutcomeCommand request, CancellationToken cancellationToken)
    {
        if (!ReplacementRequestAnswers.IsAnswer(request.Outcome))
        {
            throw new InvalidRequestException(string.Format(NotAnAnswerMessage, request.Outcome));
        }

        var row = await _repository.Get(request.Id);
        if (row is null || row.IsDeleted
            || !await _clientVisibilityGuard.AreAllVisibleAsync(
                [row.CandidateClientId, row.AbsentClientId], cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(NotFoundMessage, request.Id));
        }

        if (row.AppliedAtUtc.HasValue)
        {
            throw new InvalidRequestException(string.Format(AlreadyAppliedMessage, request.Id));
        }

        row.Outcome = request.Outcome;
        row.OutcomeAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        row.OutcomeByUserId = ReplacementRequestActor.CurrentUserId(_httpContextAccessor);
        await _unitOfWork.CompleteAsync();

        var shortNoticeHours = await ReplacementRequestSettingsReader.ReadShortNoticeHoursAsync(_settingsReader);
        return ReplacementRequestResourceMapper.ToResource(row, shortNoticeHours);
    }
}
