namespace Klacks.Api.Application.DTOs.Settings;

public class EmailTestRequest
{
    public string Server { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSSL { get; set; }
    public string AuthenticationType { get; set; } = string.Empty;
    public int Timeout { get; set; } = 10000;
}