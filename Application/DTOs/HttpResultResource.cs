namespace Klacks.Api.Application.DTOs;

public class HttpResultResource
{
    public bool Success { get; set; }

    public string Messages { get; set; } = string.Empty;
}