using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AnnotationResource?> Handle(DeleteCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAnnotation = await _annotationRepository.Get(request.Id);
            if (existingAnnotation == null)
            {
                throw new KeyNotFoundException($"Annotation with ID {request.Id} not found.");
            }

            var annotationResource = _mapper.Map<AnnotationResource>(existingAnnotation);
            await _annotationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return annotationResource;
        }, 
        "deleting annotation", 
        new { AnnotationId = request.Id });
    }
}
