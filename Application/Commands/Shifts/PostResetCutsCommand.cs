using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Shifts;

public record PostResetCutsCommand(Guid OriginalId, DateOnly NewStartDate) : IRequest<List<ShiftResource>>;
