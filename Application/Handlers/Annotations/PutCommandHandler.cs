using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AnnotationResource?> Handle(PutCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAnnotation = await _annotationRepository.Get(request.Resource.Id);
            if (existingAnnotation == null)
            {
                throw new KeyNotFoundException($"Annotation with ID {request.Resource.Id} not found.");
            }

            _mapper.Map(request.Resource, existingAnnotation);
            await _annotationRepository.Put(existingAnnotation);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AnnotationResource>(existingAnnotation);
        }, 
        "updating annotation", 
        new { AnnotationId = request.Resource.Id });
    }
}
