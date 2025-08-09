using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Queries.Annotation
{
    public record GetSimpleListQuery(Guid Id) : IRequest<IEnumerable<AnnotationResource>>;
}
