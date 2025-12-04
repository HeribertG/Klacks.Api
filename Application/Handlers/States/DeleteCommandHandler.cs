using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.States;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<StateResource>, StateResource?>
{
    private readonly IStateRepository _stateRepository;
    private readonly IMapper _mapper;
    private readonly DataBaseContext _context;

    public DeleteCommandHandler(
        IStateRepository stateRepository,
        IMapper mapper,
        DataBaseContext context,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _stateRepository = stateRepository;
        _mapper = mapper;
        _context = context;
    }

    public async Task<StateResource?> Handle(DeleteCommand<StateResource> request, CancellationToken cancellationToken)
    {
        var existingState = await _stateRepository.Get(request.Id);
        if (existingState == null)
        {
            return null;
        }

        var stateResource = _mapper.Map<StateResource>(existingState);

        await _context.State.Where(s => s.Id == request.Id).ExecuteDeleteAsync(cancellationToken);

        return stateResource;
    }
}
