using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Reports;

public record GenerateScheduleReportCommand(
    Guid ClientId,
    DateTime FromDate,
    DateTime ToDate,
    Guid? TemplateId = null) : IRequest<byte[]>;
