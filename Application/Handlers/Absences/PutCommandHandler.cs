// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates an absence type. When its on-call flag changes, the cached period hours over its absences are recomputed after
/// the commit, because an on-call absence counts no hours (IOnCallSwitchPeriodHoursRefresher).
/// </summary>
/// <param name="onCallSwitchRefresher">Recomputes the cached period hours after an on-call switch</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly SettingsMapper _settingsMapper;
    private readonly IAbsenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMultiLanguageTranslationService _translationService;
    private readonly IOnCallSwitchPeriodHoursRefresher _onCallSwitchRefresher;

    public PutCommandHandler(
                              SettingsMapper settingsMapper,
                              IAbsenceRepository repository,
                              IMultiLanguageTranslationService translationService,
                              IUnitOfWork unitOfWork,
                              IOnCallSwitchPeriodHoursRefresher onCallSwitchRefresher,
                              ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _settingsMapper = settingsMapper;
        _repository = repository;
        _translationService = translationService;
        _unitOfWork = unitOfWork;
        _onCallSwitchRefresher = onCallSwitchRefresher;
    }

    public async Task<AbsenceResource?> Handle(PutCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        await TranslateMultiLanguageFieldsAsync(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var dbAbsence = await _repository.Get(request.Resource.Id);
            if (dbAbsence == null)
            {
                throw new KeyNotFoundException($"Absence with ID {request.Resource.Id} not found.");
            }

            var wasOnCall = dbAbsence.IsOnCall;
            _settingsMapper.UpdateAbsenceEntity(request.Resource, dbAbsence);
            if (!(dbAbsence.HideInGantt && dbAbsence.AppliesToContainer))
            {
                dbAbsence.IsUnpaid = false;
            }
            await _unitOfWork.CompleteAsync();

            if (wasOnCall != dbAbsence.IsOnCall)
            {
                await _onCallSwitchRefresher.RefreshAsync(dbAbsence.Id, cancellationToken);
            }

            return _settingsMapper.ToAbsenceResource(dbAbsence);
        },
        "updating absence",
        new { AbsenceId = request.Resource.Id });
    }

    private async Task TranslateMultiLanguageFieldsAsync(AbsenceResource resource)
    {
        if (!await _translationService.IsConfiguredAsync())
        {
            return;
        }

        resource.Name = await _translationService.TranslateEmptyFieldsAsync(resource.Name);
        resource.Abbreviation = await _translationService.TranslateEmptyFieldsAsync(resource.Abbreviation);
        resource.Description = await _translationService.TranslateEmptyFieldsAsync(resource.Description);
    }
}
