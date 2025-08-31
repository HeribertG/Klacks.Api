using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<StateResource>, StateResource?>
{
    private readonly IStateRepository _stateRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IStateRepository stateRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _stateRepository = stateRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<StateResource?> Handle(DeleteCommand<StateResource> request, CancellationToken cancellationToken)
    {
        var existingState = await _stateRepository.Get(request.Id);
        if (existingState == null)
        {
            return null;
        }

        var stateResource = _mapper.Map<StateResource>(existingState);
        await _stateRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        return stateResource;
    }
}
