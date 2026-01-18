using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AbsenceDetailResource>, AbsenceDetailResource?>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IAbsenceDetailRepository absenceDetailRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceDetailResource?> Handle(PutCommand<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        var existingAbsenceDetail = await _absenceDetailRepository.Get(request.Resource.Id);
        if (existingAbsenceDetail == null)
        {
            return null;
        }

        _settingsMapper.UpdateAbsenceDetailEntity(request.Resource, existingAbsenceDetail);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToAbsenceDetailResource(existingAbsenceDetail);
    }
}
