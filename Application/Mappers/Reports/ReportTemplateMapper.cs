using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Reports;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers.Reports;

[Mapper]
public partial class ReportTemplateMapper
{
    public partial ReportTemplateResource ToResource(ReportTemplate entity);

    public partial ReportTemplate ToEntity(ReportTemplateResource resource);

    public partial List<ReportTemplateResource> ToResourceList(List<ReportTemplate> entities);

    private int MapEnum(ReportType type) => (int)type;
    private ReportType MapType(int type) => (ReportType)type;

    private int MapEnum(ReportOrientation orientation) => (int)orientation;
    private ReportOrientation MapOrientation(int orientation) => (ReportOrientation)orientation;

    private int MapEnum(ReportPageSize size) => (int)size;
    private ReportPageSize MapSize(int size) => (ReportPageSize)size;

    private int MapEnum(ReportFieldType type) => (int)type;
    private ReportFieldType MapFieldType(int type) => (ReportFieldType)type;

    private int MapEnum(ReportSectionType type) => (int)type;
    private ReportSectionType MapSectionType(int type) => (ReportSectionType)type;

    private int MapEnum(TextAlignment alignment) => (int)alignment;
    private TextAlignment MapAlignment(int alignment) => (TextAlignment)alignment;
}
