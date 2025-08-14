using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetSimpleQueryHandler : IRequestHandler<GetSimpleListQuery, IEnumerable<AnnotationResource>>
    {
        private readonly IAnnotationRepository _annotationRepository;
        private readonly IMapper _mapper;

        public GetSimpleQueryHandler(IAnnotationRepository annotationRepository, IMapper mapper)
        {
            _annotationRepository = annotationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AnnotationResource>> Handle(GetSimpleListQuery request, CancellationToken cancellationToken)
        {
            var annotations = await _annotationRepository.SimpleList(request.Id);
            return _mapper.Map<IEnumerable<AnnotationResource>>(annotations);
        }
    }
}
