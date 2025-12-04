using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetListQueryHandler : IRequestHandler<GetSimpleListQuery, IEnumerable<AnnotationResource>>
    {
        private readonly IAnnotationRepository _annotationRepository;
        private readonly SettingsMapper _settingsMapper;

        public GetListQueryHandler(IAnnotationRepository annotationRepository, SettingsMapper settingsMapper)
        {
            _annotationRepository = annotationRepository;
            _settingsMapper = settingsMapper;
        }

        public async Task<IEnumerable<AnnotationResource>> Handle(GetSimpleListQuery request, CancellationToken cancellationToken)
        {
            var annotations = await _annotationRepository.List();
            return _settingsMapper.ToAnnotationResources(annotations.ToList());
        }
    }
}
