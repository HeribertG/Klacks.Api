using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations
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
