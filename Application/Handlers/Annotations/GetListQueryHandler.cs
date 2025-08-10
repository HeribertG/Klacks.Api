using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations
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

            return mapper.Map<List<Klacks.Api.Domain.Models.Staffs.Annotation>, List<AnnotationResource>>(annotation!);
        }
    }
}
