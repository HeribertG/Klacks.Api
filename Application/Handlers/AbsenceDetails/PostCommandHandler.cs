using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AbsenceDetailResource>, AbsenceDetailResource?>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IAbsenceDetailRepository absenceDetailRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceDetailResource?> Handle(PostCommand<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        var absenceDetail = _settingsMapper.ToAbsenceDetailEntity(request.Resource);
        await _absenceDetailRepository.Add(absenceDetail);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToAbsenceDetailResource(absenceDetail);
    }
}
