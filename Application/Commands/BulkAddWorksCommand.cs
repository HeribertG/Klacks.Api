using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Commands;

public record BulkAddWorksCommand(BulkAddWorksRequest Request) : IRequest<BulkWorksResponse>;
