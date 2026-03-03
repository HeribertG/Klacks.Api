// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.States;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<StateResource>, StateResource?>
{
    private readonly IStateRepository _stateRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IStateRepository stateRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _stateRepository = stateRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<StateResource?> Handle(PostCommand<StateResource> request, CancellationToken cancellationToken)
    {
        var state = _settingsMapper.ToStateEntity(request.Resource);
        await _stateRepository.Add(state);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToStateResource(state);
    }
}
