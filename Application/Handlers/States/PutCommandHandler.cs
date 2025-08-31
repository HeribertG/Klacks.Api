using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<StateResource>, StateResource?>
{
    private readonly IStateRepository _stateRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IStateRepository stateRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _stateRepository = stateRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<StateResource?> Handle(PutCommand<StateResource> request, CancellationToken cancellationToken)
    {
        var existingState = await _stateRepository.Get(request.Resource.Id);
        if (existingState == null)
        {
            return null;
        }

        _mapper.Map(request.Resource, existingState);
        await _stateRepository.Put(existingState);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<StateResource>(existingState);
    }
}
