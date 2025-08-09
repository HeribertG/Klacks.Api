using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Queries.Annotation
{
    public record GetSimpleListQuery(Guid Id) : IRequest<IEnumerable<AnnotationResource>>;
}
