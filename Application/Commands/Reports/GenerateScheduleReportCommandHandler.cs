using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Reports;

public class GenerateScheduleReportCommandHandler : IRequestHandler<GenerateScheduleReportCommand, byte[]>
{
    private readonly IReportGenerator _reportGenerator;
    private readonly IReportTemplateRepository _templateRepository;

    public GenerateScheduleReportCommandHandler(
        IReportGenerator reportGenerator,
        IReportTemplateRepository templateRepository)
    {
        _reportGenerator = reportGenerator;
        _templateRepository = templateRepository;
    }

    public async Task<byte[]> Handle(GenerateScheduleReportCommand request, CancellationToken cancellationToken)
    {
        Domain.Entities.Reports.ReportTemplate? template = null;

        if (request.TemplateId.HasValue)
        {
            template = await _templateRepository.GetByIdAsync(request.TemplateId.Value, cancellationToken);
        }

        return await _reportGenerator.GenerateScheduleReportAsync(
            request.ClientId,
            request.FromDate,
            request.ToDate,
            template,
            cancellationToken);
    }
}
