using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Entities.Reports;

public class ReportTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public ReportPageSetup PageSetup { get; set; } = new();
    public List<ReportSection> Sections { get; set; } = [];
}

public enum ReportType
{
    Schedule = 0,
    Client = 1,
    Invoice = 2,
    Absence = 3
}

public class ReportPageSetup
{
    public ReportOrientation Orientation { get; set; } = ReportOrientation.Portrait;
    public ReportPageSize Size { get; set; } = ReportPageSize.A4;
    public ReportMargins Margins { get; set; } = new();
}

public enum ReportOrientation
{
    Portrait = 0,
    Landscape = 1
}

public enum ReportPageSize
{
    A4 = 0,
    A3 = 1,
    Letter = 2
}

public class ReportMargins
{
    public float Top { get; set; } = 20;
    public float Bottom { get; set; } = 20;
    public float Left { get; set; } = 20;
    public float Right { get; set; } = 20;
}
