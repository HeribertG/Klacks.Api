using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Reports;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class GetReportTemplatesByTypeQueryHandler : IRequestHandler<GetReportTemplatesByTypeQuery, IEnumerable<ReportTemplate>>
{
    private readonly IReportTemplateRepository _templateRepository;

    public GetReportTemplatesByTypeQueryHandler(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<IEnumerable<ReportTemplate>> Handle(GetReportTemplatesByTypeQuery request, CancellationToken cancellationToken)
    {
        return await _templateRepository.GetByTypeAsync(request.Type, cancellationToken);
    }
}
