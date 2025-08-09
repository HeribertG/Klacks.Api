using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Annotations;

public class PostCommandHandler : IRequestHandler<PostCommand<AnnotationResource>, AnnotationResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IAnnotationRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IAnnotationRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<AnnotationResource?> Handle(PostCommand<AnnotationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var annotation = mapper.Map<AnnotationResource, Models.Staffs.Annotation>(request.Resource);
            await repository.Add(annotation);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New annotation added successfully. ID: {AnnotationId}", annotation.Id);

            return mapper.Map<Models.Staffs.Annotation, AnnotationResource>(annotation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new annotation. ID: {AnnotationId}", request.Resource.Id);
            throw;
        }
    }
}
