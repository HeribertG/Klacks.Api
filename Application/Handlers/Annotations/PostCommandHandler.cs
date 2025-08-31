using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IAnnotationRepository annotationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AnnotationResource?> Handle(PostCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var annotation = _mapper.Map<Annotation>(request.Resource);
            await _annotationRepository.Add(annotation);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AnnotationResource>(annotation);
        }, 
        "creating annotation", 
        new { AnnotationId = request.Resource?.Id });
    }
}
