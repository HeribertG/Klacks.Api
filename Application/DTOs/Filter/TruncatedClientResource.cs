using Klacks.Api.Application.DTOs.Clients;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedClientResource : BaseTruncatedResult
{
    public ICollection<ClientListItemResource>? Clients { get; set; }

    public string Editor { get; set; } = string.Empty;

    public DateTime LastChange { get; set; }
}
