using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Commands.Shifts;

public record PostCutsCommand(List<ShiftResource> Cuts) : IRequest<List<ShiftResource>>;