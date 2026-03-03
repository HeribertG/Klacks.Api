// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.States;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<StateResource>, StateResource?>
{
    private readonly IStateRepository _stateRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IStateRepository stateRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _stateRepository = stateRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<StateResource?> Handle(DeleteCommand<StateResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting state with ID: {StateId}", request.Id);

        var existingState = await _stateRepository.Get(request.Id);
        if (existingState == null)
        {
            _logger.LogWarning("State not found: {StateId}", request.Id);
            return null;
        }

        var stateResource = _settingsMapper.ToStateResource(existingState);

        await _stateRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("State deleted successfully: {StateId}", request.Id);
        return stateResource;
    }
}
