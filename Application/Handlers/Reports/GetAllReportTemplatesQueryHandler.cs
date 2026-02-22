// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Reports;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class GetAllReportTemplatesQueryHandler : IRequestHandler<GetAllReportTemplatesQuery, IEnumerable<ReportTemplate>>
{
    private readonly IReportTemplateRepository _templateRepository;

    public GetAllReportTemplatesQueryHandler(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<IEnumerable<ReportTemplate>> Handle(GetAllReportTemplatesQuery request, CancellationToken cancellationToken)
    {
        return await _templateRepository.GetAllAsync(cancellationToken);
    }
}
