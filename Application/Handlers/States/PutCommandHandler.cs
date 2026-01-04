using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.States;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<StateResource>, StateResource?>
{
    private readonly IStateRepository _stateRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IStateRepository stateRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _stateRepository = stateRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<StateResource?> Handle(PutCommand<StateResource> request, CancellationToken cancellationToken)
    {
        var existingState = await _stateRepository.Get(request.Resource.Id);
        if (existingState == null)
        {
            return null;
        }

        _settingsMapper.UpdateStateEntity(request.Resource, existingState);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToStateResource(existingState);
    }
}
