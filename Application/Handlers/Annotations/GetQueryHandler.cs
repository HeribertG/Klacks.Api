using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Annotations
{
    public class GetQueryHandler : IRequestHandler<GetQuery<AnnotationResource>, AnnotationResource?>
    {
        private readonly AnnotationApplicationService _annotationApplicationService;

        public GetQueryHandler(AnnotationApplicationService annotationApplicationService)
        {
            _annotationApplicationService = annotationApplicationService;
        }

        public async Task<AnnotationResource?> Handle(GetQuery<AnnotationResource> request, CancellationToken cancellationToken)
        {
            return await _annotationApplicationService.GetAnnotationByIdAsync(request.Id, cancellationToken);
        }
    }
}
