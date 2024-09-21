using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Annotations;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<AnnotationResource>, AnnotationResource?>
{
  private readonly ILogger<DeleteCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IAnnotationRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public DeleteCommandHandler(
                              IMapper mapper,
                              IAnnotationRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<DeleteCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<AnnotationResource?> Handle(DeleteCommand<AnnotationResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var annotation = await repository.Delete(request.Id);
      if (annotation == null)
      {
        logger.LogWarning("Annotation with ID {AnnotationId} not found for deletion.", request.Id);
        return null;
      }

      await unitOfWork.CompleteAsync();

      logger.LogInformation("Annotation with ID {AnnotationId} deleted successfully.", request.Id);

      return mapper.Map<Models.Staffs.Annotation, AnnotationResource>(annotation);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while deleting annotation with ID {AnnotationId}.", request.Id);
      throw;
    }
  }
}
