using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.Absences;

public record VisibleAbsencesQuery : IRequest<List<AbsenceResource>>;
