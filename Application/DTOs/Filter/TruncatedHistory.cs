using Klacks.Api.Domain.Models.Histories;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class TruncatedHistory
{
    public int MaxItems { get; set; }
    
    public int MaxPages { get; set; }
    
    public int CurrentPage { get; set; }
    
    public ICollection<History>? Histories { get; set; }
}
