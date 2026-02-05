using Klacks.Api.Domain.Models.Reports;

namespace Klacks.Api.Application.Interfaces;

public interface IReportGenerator
{
    Task<byte[]> GenerateScheduleReportAsync(
        Guid clientId,
        DateTime fromDate,
        DateTime toDate,
        ReportTemplate? template = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateClientReportAsync(
        Guid clientId,
        ReportTemplate? template = null,
        CancellationToken cancellationToken = default);
}
