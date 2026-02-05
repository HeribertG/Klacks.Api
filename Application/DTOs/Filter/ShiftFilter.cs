using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class ShiftFilter : BaseFilter
{
    public bool ActiveDateRange { get; set; }

    public bool FormerDateRange { get; set; }

    public bool FutureDateRange { get; set; }

    public string SearchString { get; set; } = string.Empty;

    public ShiftFilterType FilterType { get; set; } = ShiftFilterType.Original;

    public bool IncludeClientName { get; set; }

    public bool IsSealedOrder { get; set; }

    public bool IsTimeRange { get; set; }

    public bool IsSporadic { get; set; }

    public Guid? SelectedGroup { get; set; }
}
