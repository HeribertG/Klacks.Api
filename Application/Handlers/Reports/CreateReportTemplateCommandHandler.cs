using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class CreateReportTemplateCommandHandler : IRequestHandler<CreateReportTemplateCommand, ReportTemplate>
{
    private readonly IReportTemplateRepository _templateRepository;

    public CreateReportTemplateCommandHandler(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<ReportTemplate> Handle(CreateReportTemplateCommand request, CancellationToken cancellationToken)
    {
        return await _templateRepository.CreateAsync(request.Template, cancellationToken);
    }
}
