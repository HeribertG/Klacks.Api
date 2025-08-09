using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Annotation;
using Klacks.Api.Presentation.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Annotations
{
    public class GetListQueryHandler : IRequestHandler<GetSimpleListQuery, IEnumerable<AnnotationResource>>
    {
        private readonly IMapper mapper;
        private readonly IAnnotationRepository repository;

        public GetListQueryHandler(IMapper mapper,
                                   IAnnotationRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<AnnotationResource>> Handle(GetSimpleListQuery request, CancellationToken cancellationToken)
        {
            var annotation = await repository.List();

            return mapper.Map<List<Models.Staffs.Annotation>, List<AnnotationResource>>(annotation!);
        }
    }
}
