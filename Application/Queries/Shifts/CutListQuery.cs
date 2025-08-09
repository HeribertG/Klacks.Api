using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Queries.Shifts;

public record CutListQuery(Guid Id) : IRequest<IEnumerable<ShiftResource>>;
