using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Staffs;
using MediatR;

namespace Klacks_api.Handlers.Annotations
{
  public class GetQueryHandler : IRequestHandler<GetQuery<AnnotationResource>, AnnotationResource?>
  {
    private readonly IMapper mapper;
    private readonly IAnnotationRepository repository;

    public GetQueryHandler(IMapper mapper,
                           IAnnotationRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<AnnotationResource?> Handle(GetQuery<AnnotationResource> request, CancellationToken cancellationToken)
    {
      var annotation = await repository.Get(request.Id);
      return mapper.Map<Models.Staffs.Annotation, AnnotationResource>(annotation!);
    }
  }
}
