namespace Klacks.Api.Application.DTOs.Reports;

public class ReportTemplateResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Type { get; set; }
    public ReportPageSetupResource PageSetup { get; set; } = new();
    public List<ReportSectionResource> Sections { get; set; } = [];
}
