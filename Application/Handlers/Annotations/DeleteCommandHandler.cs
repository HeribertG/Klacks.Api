using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AnnotationResource?> Handle(DeleteCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAnnotation = await _annotationRepository.Get(request.Id);
            if (existingAnnotation == null)
            {
                _logger.LogWarning("Annotation with ID {AnnotationId} not found for deletion.", request.Id);
                return null;
            }

            var annotationResource = _mapper.Map<AnnotationResource>(existingAnnotation);
            await _annotationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return annotationResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting annotation with ID {AnnotationId}.", request.Id);
            throw;
        }
    }
}
