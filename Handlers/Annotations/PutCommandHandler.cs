using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Annotations;

public class PutCommandHandler : IRequestHandler<PutCommand<AnnotationResource>, AnnotationResource?>
{
  private readonly ILogger<PutCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IAnnotationRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PutCommandHandler(
                            IMapper mapper,
                            IAnnotationRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PutCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<AnnotationResource?> Handle(PutCommand<AnnotationResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var dbAnnotation = await repository.Get(request.Resource.Id);
      if (dbAnnotation == null)
      {
        logger.LogWarning("Annotation with ID {AnnotationId} not found.", request.Resource.Id);
        return null;
      }

      var updatedAnnotation = mapper.Map(request.Resource, dbAnnotation);
      updatedAnnotation = repository.Put(updatedAnnotation);
      await unitOfWork.CompleteAsync();

      logger.LogInformation("Annotation with ID {AnnotationId} updated successfully.", request.Resource.Id);

      return mapper.Map<Models.Staffs.Annotation, AnnotationResource>(updatedAnnotation);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while updating annotation with ID {AnnotationId}.", request.Resource.Id);
      throw;
    }
  }
}
