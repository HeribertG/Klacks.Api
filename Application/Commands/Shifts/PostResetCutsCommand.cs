using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Commands.Shifts;

public record PostResetCutsCommand(Guid OriginalId, DateOnly NewStartDate) : IRequest<List<ShiftResource>>;
