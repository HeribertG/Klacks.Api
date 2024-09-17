using Klacks_api.Resources.Staffs;
using MediatR;

namespace Klacks_api.Queries.Annotation
{
  public record GetSimpleListQuery(Guid Id) : IRequest<IEnumerable<AnnotationResource>>;
}
