using Klacks.Api.Application.Commands.GroupVisibilities;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class BulkGroupVisibilitiesCommandHandler : IRequestHandler<BulkGroupVisibilitiesCommand>
{
    private readonly ILogger<BulkGroupVisibilitiesCommandHandler> _logger; 
    private readonly GroupVisibilityApplicationService _groupVisibilityApplicationService;
    private readonly IUnitOfWork _unitOfWork;

    public BulkGroupVisibilitiesCommandHandler(
        ILogger<BulkGroupVisibilitiesCommandHandler> logger,
        GroupVisibilityApplicationService groupVisibilityApplicationService,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _groupVisibilityApplicationService = groupVisibilityApplicationService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(BulkGroupVisibilitiesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bulk update of GroupVisibility list with {Count} items.", request.List.Count);

        try
        {
            await _groupVisibilityApplicationService.SetGroupVisibilityListAsync(request.List, cancellationToken);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Bulk update of GroupVisibility list completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating GroupVisibility list.");
            throw;
        }
    }
}
