// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Reports;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class GetReportTemplateByIdQueryHandler : IRequestHandler<GetReportTemplateByIdQuery, ReportTemplate?>
{
    private readonly IReportTemplateRepository _templateRepository;

    public GetReportTemplateByIdQueryHandler(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<ReportTemplate?> Handle(GetReportTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        return await _templateRepository.GetByIdAsync(request.Id, cancellationToken);
    }
}
