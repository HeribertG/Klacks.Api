using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Queries.Shifts;

public record CutListQuery(Guid Id) : IRequest<IEnumerable<ShiftResource>>;
