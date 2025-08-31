using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IBreakRepository breakRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingBreak = await _breakRepository.Get(request.Resource.Id);
            if (existingBreak == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found.");
            }

            _mapper.Map(request.Resource, existingBreak);
            await _breakRepository.Put(existingBreak);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<BreakResource>(existingBreak);
        }, 
        "updating break", 
        new { BreakId = request.Resource.Id });
    }
}
