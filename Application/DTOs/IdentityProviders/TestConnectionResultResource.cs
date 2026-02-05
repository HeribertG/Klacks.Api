namespace Klacks.Api.Application.DTOs.IdentityProviders;

public class TestConnectionResultResource
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public int? UserCount { get; set; }

    public List<string>? SampleUsers { get; set; }
}
