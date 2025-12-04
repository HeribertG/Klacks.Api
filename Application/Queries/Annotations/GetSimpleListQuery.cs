using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Annotation
{
    public record GetSimpleListQuery(Guid Id) : IRequest<IEnumerable<AnnotationResource>>;
}
