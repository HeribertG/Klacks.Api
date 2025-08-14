using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IBreakRepository breakRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _breakRepository = breakRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingBreak = await _breakRepository.Get(request.Resource.Id);
            if (existingBreak == null)
            {
                _logger.LogWarning("Break with ID {BreakId} not found.", request.Resource.Id);
                return null;
            }

            _mapper.Map(request.Resource, existingBreak);
            await _breakRepository.Put(existingBreak);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<BreakResource>(existingBreak);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating break with ID {BreakId}.", request.Resource.Id);
            throw;
        }
    }
}
