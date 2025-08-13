using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetListQueryHandler : IRequestHandler<GetSimpleListQuery, IEnumerable<AnnotationResource>>
    {
        private readonly AnnotationApplicationService _annotationApplicationService;

        public GetListQueryHandler(AnnotationApplicationService annotationApplicationService)
        {
            _annotationApplicationService = annotationApplicationService;
        }

        public async Task<IEnumerable<AnnotationResource>> Handle(GetSimpleListQuery request, CancellationToken cancellationToken)
        {
            return await _annotationApplicationService.GetAllAnnotationsAsync(cancellationToken);
        }
    }
}
