using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Annotations;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IAnnotationRepository annotationRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _settingsMapper = settingsMapper;
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

            var annotationResource = _settingsMapper.ToAnnotationResource(existingAnnotation);
            await _annotationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return annotationResource;
        }, 
        "deleting annotation", 
        new { AnnotationId = request.Id });
    }
}
