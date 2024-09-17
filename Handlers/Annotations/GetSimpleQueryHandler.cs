using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Annotation;
using Klacks_api.Resources.Staffs;
using MediatR;

namespace Klacks_api.Handlers.Annotations
{
  public class GetSimpleQueryHandler : IRequestHandler<GetSimpleListQuery, IEnumerable<AnnotationResource>>
  {
    private readonly IMapper mapper;
    private readonly IAnnotationRepository repository;

    public GetSimpleQueryHandler(IMapper mapper,
                               IAnnotationRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<AnnotationResource>> Handle(GetSimpleListQuery request, CancellationToken cancellationToken)
    {
      var annotations = await repository.SimpleList(request.Id);
      return mapper.Map<List<Models.Staffs.Annotation>, List<AnnotationResource>>(annotations!);
    }
  }
}
