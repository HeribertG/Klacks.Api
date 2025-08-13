using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PutCommandHandler : IRequestHandler<PutCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly AnnotationApplicationService _annotationApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        AnnotationApplicationService annotationApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _annotationApplicationService = annotationApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AnnotationResource?> Handle(PutCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAnnotation = await _annotationApplicationService.GetAnnotationByIdAsync(request.Resource.Id, cancellationToken);
            if (existingAnnotation == null)
            {
                _logger.LogWarning("Annotation with ID {AnnotationId} not found.", request.Resource.Id);
                return null;
            }

            var result = await _annotationApplicationService.UpdateAnnotationAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating annotation with ID {AnnotationId}.", request.Resource.Id);
            throw;
        }
    }
}
