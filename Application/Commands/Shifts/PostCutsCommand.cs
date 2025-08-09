using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Commands.Shifts;

public record PostCutsCommand(List<ShiftResource> Cuts) : IRequest<List<ShiftResource>>;