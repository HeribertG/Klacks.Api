using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Queries.Absences
{
    public record TruncatedListQuery(AbsenceFilter Filter) : IRequest<TruncatedAbsence>;
}
