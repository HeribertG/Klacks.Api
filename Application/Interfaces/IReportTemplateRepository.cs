// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces;

public interface IReportTemplateRepository
{
    Task<IEnumerable<ReportTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ReportTemplate>> GetByTypeAsync(ReportType type, CancellationToken cancellationToken = default);
    Task<ReportTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReportTemplate> CreateAsync(ReportTemplate template, CancellationToken cancellationToken = default);
    Task<ReportTemplate> UpdateAsync(ReportTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
