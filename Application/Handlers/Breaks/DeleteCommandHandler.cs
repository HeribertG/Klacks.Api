using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IBreakRepository breakRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _breakRepository = breakRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BreakResource?> Handle(DeleteCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingBreak = await _breakRepository.Get(request.Id);
            if (existingBreak == null)
            {
                _logger.LogWarning("Break with ID {BreakId} not found for deletion.", request.Id);
                return null;
            }

            var breakResource = _mapper.Map<BreakResource>(existingBreak);
            await _breakRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return breakResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting break with ID {BreakId}.", request.Id);
            throw;
        }
    }
}
