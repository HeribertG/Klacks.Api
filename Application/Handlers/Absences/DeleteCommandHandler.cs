using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly SettingsMapper _settingsMapper;
    private readonly IAbsenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        SettingsMapper settingsMapper,
        IAbsenceRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _settingsMapper = settingsMapper;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceResource?> Handle(DeleteCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var absence = await _repository.Get(request.Id);
            if (absence == null)
            {
                throw new KeyNotFoundException($"Absence with ID {request.Id} not found.");
            }

            var absenceResource = _settingsMapper.ToAbsenceResource(absence);
            await _repository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            return absenceResource;
        },
        "deleting absence",
        new { AbsenceId = request.Id });
    }
}
