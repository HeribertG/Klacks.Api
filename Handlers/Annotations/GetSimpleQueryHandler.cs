using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Annotation;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Annotations
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
