using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences;

public class PostCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IMultiLanguageTranslationService _translationService;

    public PostCommandHandler(
        IAbsenceRepository absenceRepository,
        SettingsMapper settingsMapper,
        IMultiLanguageTranslationService translationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _absenceRepository = absenceRepository;
        _settingsMapper = settingsMapper;
        _translationService = translationService;
    }

    public async Task<AbsenceResource?> Handle(PostCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        ValidateAbsenceRequest(request.Resource);
        await TranslateMultiLanguageFieldsAsync(request.Resource!);

        return await ExecuteWithTransactionAsync(async () =>
        {
            var absence = _settingsMapper.ToAbsenceEntity(request.Resource);
            await _absenceRepository.Add(absence);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToAbsenceResource(absence);
        },
        "creating absence",
        new { AbsenceId = request.Resource?.Id });
    }

    private async Task TranslateMultiLanguageFieldsAsync(AbsenceResource resource)
    {
        if (!_translationService.IsConfigured)
        {
            return;
        }

        resource.Name = await _translationService.TranslateEmptyFieldsAsync(resource.Name);
        resource.Abbreviation = await _translationService.TranslateEmptyFieldsAsync(resource.Abbreviation);
        resource.Description = await _translationService.TranslateEmptyFieldsAsync(resource.Description);
    }

    private void ValidateAbsenceRequest(AbsenceResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Absence data is required.");
        }

        if (resource.Name == null ||
            (string.IsNullOrWhiteSpace(resource.Name.De) &&
             string.IsNullOrWhiteSpace(resource.Name.En) &&
             string.IsNullOrWhiteSpace(resource.Name.Fr) &&
             string.IsNullOrWhiteSpace(resource.Name.It)))
        {
            throw new InvalidRequestException("Absence name is required in at least one language.");
        }
    }
}
