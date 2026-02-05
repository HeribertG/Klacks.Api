using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Shifts;

public record PostBatchCutsCommand(List<CutOperation> Operations) : IRequest<List<ShiftResource>>;
