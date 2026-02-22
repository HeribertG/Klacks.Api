using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Annotations;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly IAnnotationRepository _annotationRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IAnnotationRepository annotationRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _annotationRepository = annotationRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AnnotationResource?> Handle(PostCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var annotation = _settingsMapper.ToAnnotationEntity(request.Resource);
            await _annotationRepository.Add(annotation);
            await _unitOfWork.CompleteAsync();
            return _settingsMapper.ToAnnotationResource(annotation);
        }, 
        "creating annotation", 
        new { AnnotationId = request.Resource?.Id });
    }
}
