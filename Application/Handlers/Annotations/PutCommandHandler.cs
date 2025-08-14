using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PutCommandHandler : IRequestHandler<PutCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AnnotationResource?> Handle(PutCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAnnotation = await _annotationRepository.Get(request.Resource.Id);
            if (existingAnnotation == null)
            {
                _logger.LogWarning("Annotation with ID {AnnotationId} not found.", request.Resource.Id);
                return null;
            }

            _mapper.Map(request.Resource, existingAnnotation);
            await _annotationRepository.Put(existingAnnotation);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AnnotationResource>(existingAnnotation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating annotation with ID {AnnotationId}.", request.Resource.Id);
            throw;
        }
    }
}
