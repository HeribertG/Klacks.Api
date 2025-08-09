using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Annotations
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
