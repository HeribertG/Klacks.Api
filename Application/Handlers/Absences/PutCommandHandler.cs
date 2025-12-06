using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly SettingsMapper _settingsMapper;
    private readonly IAbsenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
                              SettingsMapper settingsMapper,
                              IAbsenceRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _settingsMapper = settingsMapper;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceResource?> Handle(PutCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var dbAbsence = await _repository.Get(request.Resource.Id);
            if (dbAbsence == null)
            {
                throw new KeyNotFoundException($"Absence with ID {request.Resource.Id} not found.");
            }

            _settingsMapper.UpdateAbsenceEntity(request.Resource, dbAbsence);
            await _unitOfWork.CompleteAsync();

            return _settingsMapper.ToAbsenceResource(dbAbsence);
        },
        "updating absence",
        new { AbsenceId = request.Resource.Id });
    }
}
