using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.States;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<StateResource>, StateResource?>
{
    private readonly IStateRepository _stateRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IStateRepository stateRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _stateRepository = stateRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<StateResource?> Handle(PostCommand<StateResource> request, CancellationToken cancellationToken)
    {
        var state = _mapper.Map<State>(request.Resource);
        await _stateRepository.Add(state);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<StateResource>(state);
    }
}
