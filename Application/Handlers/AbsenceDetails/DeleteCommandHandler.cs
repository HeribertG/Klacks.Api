using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AbsenceDetailResource>, AbsenceDetailResource?>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IAbsenceDetailRepository absenceDetailRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceDetailResource?> Handle(DeleteCommand<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting absence detail with ID: {AbsenceDetailId}", request.Id);

        var existingAbsenceDetail = await _absenceDetailRepository.Get(request.Id);
        if (existingAbsenceDetail == null)
        {
            _logger.LogWarning("AbsenceDetail not found: {AbsenceDetailId}", request.Id);
            return null;
        }

        var absenceDetailResource = _settingsMapper.ToAbsenceDetailResource(existingAbsenceDetail);

        await _absenceDetailRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("AbsenceDetail deleted successfully: {AbsenceDetailId}", request.Id);
        return absenceDetailResource;
    }
}
