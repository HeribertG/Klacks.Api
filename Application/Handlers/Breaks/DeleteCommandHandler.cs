using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Breaks;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IBreakRepository breakRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakResource?> Handle(DeleteCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingBreak = await _breakRepository.Get(request.Id);
            if (existingBreak == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found.");
            }

            var breakResource = _mapper.Map<BreakResource>(existingBreak);
            await _breakRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return breakResource;
        }, 
        "deleting break", 
        new { BreakId = request.Id });
    }
}
