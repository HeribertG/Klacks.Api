using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<BreakResource>, BreakResource?>
{
    private readonly BreakApplicationService _breakApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        BreakApplicationService breakApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _breakApplicationService = breakApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BreakResource?> Handle(DeleteCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingBreak = await _breakApplicationService.GetBreakByIdAsync(request.Id, cancellationToken);
            if (existingBreak == null)
            {
                _logger.LogWarning("Break with ID {BreakId} not found for deletion.", request.Id);
                return null;
            }

            await _breakApplicationService.DeleteBreakAsync(request.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return existingBreak;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting break with ID {BreakId}.", request.Id);
            throw;
        }
    }
}
