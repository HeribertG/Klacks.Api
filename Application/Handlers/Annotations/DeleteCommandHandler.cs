using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly AnnotationApplicationService _annotationApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        AnnotationApplicationService annotationApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _annotationApplicationService = annotationApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AnnotationResource?> Handle(DeleteCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAnnotation = await _annotationApplicationService.GetAnnotationByIdAsync(request.Id, cancellationToken);
            if (existingAnnotation == null)
            {
                _logger.LogWarning("Annotation with ID {AnnotationId} not found for deletion.", request.Id);
                return null;
            }

            await _annotationApplicationService.DeleteAnnotationAsync(request.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return existingAnnotation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting annotation with ID {AnnotationId}.", request.Id);
            throw;
        }
    }
}
