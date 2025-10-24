using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Commands.Shifts;

public record PostBatchCutsCommand(List<CutOperation> Operations) : IRequest<List<ShiftResource>>;
