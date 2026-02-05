using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class UpdateReportTemplateCommandHandler : IRequestHandler<UpdateReportTemplateCommand, ReportTemplate>
{
    private readonly IReportTemplateRepository _templateRepository;

    public UpdateReportTemplateCommandHandler(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<ReportTemplate> Handle(UpdateReportTemplateCommand request, CancellationToken cancellationToken)
    {
        return await _templateRepository.UpdateAsync(request.Template, cancellationToken);
    }
}
