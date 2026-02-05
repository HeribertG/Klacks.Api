using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AbsenceDetailResource>, AbsenceDetailResource?>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMultiLanguageTranslationService _translationService;

    public PutCommandHandler(
        IAbsenceDetailRepository absenceDetailRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        IMultiLanguageTranslationService translationService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
        _translationService = translationService;
    }

    public async Task<AbsenceDetailResource?> Handle(PutCommand<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        await TranslateMultiLanguageFieldsAsync(request.Resource);

        var existingAbsenceDetail = await _absenceDetailRepository.Get(request.Resource.Id);
        if (existingAbsenceDetail == null)
        {
            return null;
        }

        _settingsMapper.UpdateAbsenceDetailEntity(request.Resource, existingAbsenceDetail);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToAbsenceDetailResource(existingAbsenceDetail);
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
