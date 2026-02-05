using Klacks.Api.Presentation.DTOs.Clients;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class TruncatedClientResource : BaseTruncatedResult
{
    public ICollection<ClientListItemResource>? Clients { get; set; }

    public string Editor { get; set; } = string.Empty;

    public DateTime LastChange { get; set; }
}
