using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IAnnotationRepository annotationRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _settingsMapper = settingsMapper;
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

            _settingsMapper.UpdateAnnotationEntity(request.Resource, existingAnnotation);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToAnnotationResource(existingAnnotation);
        },
        "updating annotation",
        new { AnnotationId = request.Resource.Id });
    }
}
