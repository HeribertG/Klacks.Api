using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class TruncatedClientResource : BaseTruncatedResult
{
    public ICollection<ClientResource>? Clients { get; set; }

    public string Editor { get; set; } = string.Empty;
    
    public DateTime LastChange { get; set; }
}
