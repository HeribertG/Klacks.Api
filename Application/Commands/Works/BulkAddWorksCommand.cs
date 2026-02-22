using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Works;

public record BulkAddWorksCommand(BulkAddWorksRequest Request) : IRequest<BulkWorksResponse>;
