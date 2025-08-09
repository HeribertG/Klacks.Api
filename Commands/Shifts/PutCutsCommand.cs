using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Commands.Shifts;

public record PutCutsCommand(List<ShiftResource> Cuts) : IRequest<List<ShiftResource>>;