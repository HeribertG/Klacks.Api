using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Commands.Shifts;

public record PutCutsCommand(List<ShiftResource> Cuts) : IRequest<List<ShiftResource>>;