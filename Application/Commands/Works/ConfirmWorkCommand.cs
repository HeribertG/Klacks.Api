using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Works;

public record ConfirmWorkCommand(Guid WorkId) : IRequest<WorkResource?>;
