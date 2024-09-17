using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Queries.Absences
{
  public record TruncatedListQuery(AbsenceFilter Filter) : IRequest<TruncatedAbsence>;
}
