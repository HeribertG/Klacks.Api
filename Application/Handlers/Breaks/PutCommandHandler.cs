using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly BreakApplicationService _breakApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        BreakApplicationService breakApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _breakApplicationService = breakApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingBreak = await _breakApplicationService.GetBreakByIdAsync(request.Resource.Id, cancellationToken);
            if (existingBreak == null)
            {
                _logger.LogWarning("Break with ID {BreakId} not found.", request.Resource.Id);
                return null;
            }

            var result = await _breakApplicationService.UpdateBreakAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating break with ID {BreakId}.", request.Resource.Id);
            throw;
        }
    }
}
