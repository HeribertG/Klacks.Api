// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AbsenceDetailResource>, AbsenceDetailResource?>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMultiLanguageTranslationService _translationService;

    public PostCommandHandler(
        IAbsenceDetailRepository absenceDetailRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        IMultiLanguageTranslationService translationService,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
        _translationService = translationService;
    }

    public async Task<AbsenceDetailResource?> Handle(PostCommand<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        await TranslateMultiLanguageFieldsAsync(request.Resource);

        var absenceDetail = _settingsMapper.ToAbsenceDetailEntity(request.Resource);
        await _absenceDetailRepository.Add(absenceDetail);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToAbsenceDetailResource(absenceDetail);
    }

    private async Task TranslateMultiLanguageFieldsAsync(AbsenceDetailResource resource)
    {
        if (!_translationService.IsConfigured)
        {
            return;
        }

        resource.DetailName = await _translationService.TranslateEmptyFieldsAsync(resource.DetailName);
        resource.Description = await _translationService.TranslateEmptyFieldsAsync(resource.Description);
    }
}
