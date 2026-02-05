namespace Klacks.Api.Presentation.DTOs.Clients;

public class ClientListItemResource
{
    public string Id { get; set; } = string.Empty;
    public int IdNumber { get; set; }
    public string? Company { get; set; } = string.Empty;
    public string? FirstName { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public bool IsDeleted { get; set; }
}
