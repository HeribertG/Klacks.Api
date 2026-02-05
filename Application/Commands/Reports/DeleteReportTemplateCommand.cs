using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Reports;

public record DeleteReportTemplateCommand(Guid Id) : IRequest;
