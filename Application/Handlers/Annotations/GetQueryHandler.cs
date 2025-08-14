using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetQueryHandler : IRequestHandler<GetQuery<AnnotationResource>, AnnotationResource?>
    {
        private readonly IAnnotationRepository _annotationRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IAnnotationRepository annotationRepository, IMapper mapper)
        {
            _annotationRepository = annotationRepository;
            _mapper = mapper;
        }

        public async Task<AnnotationResource?> Handle(GetQuery<AnnotationResource> request, CancellationToken cancellationToken)
        {
            var annotation = await _annotationRepository.Get(request.Id);
            return annotation != null ? _mapper.Map<AnnotationResource>(annotation) : null;
        }
    }
}
