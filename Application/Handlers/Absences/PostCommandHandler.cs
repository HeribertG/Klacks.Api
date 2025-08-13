using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Absences;

public class PostCommandHandler : IRequestHandler<PostCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly AbsenceApplicationService _absenceApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        AbsenceApplicationService absenceApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _absenceApplicationService = absenceApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AbsenceResource?> Handle(PostCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing create absence command");
            
            var result = await _absenceApplicationService.CreateAbsenceAsync(request.Resource, cancellationToken);
            
            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync(transaction);
            
            _logger.LogInformation("New absence added successfully. ID: {AbsenceId}", result.Id);
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error occurred while adding a new absence. ID: {AbsenceId}", request.Resource.Id);
            throw;
        }
    }
}
