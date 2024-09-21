using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Queries.Annotation
{
  public record GetSimpleListQuery(Guid Id) : IRequest<IEnumerable<AnnotationResource>>;
}
