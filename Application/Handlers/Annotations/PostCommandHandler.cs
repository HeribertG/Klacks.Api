using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PostCommandHandler : IRequestHandler<PostCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly AnnotationApplicationService _annotationApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        AnnotationApplicationService annotationApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _annotationApplicationService = annotationApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AnnotationResource?> Handle(PostCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _annotationApplicationService.CreateAnnotationAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new annotation. ID: {AnnotationId}", request.Resource.Id);
            throw;
        }
    }
}
