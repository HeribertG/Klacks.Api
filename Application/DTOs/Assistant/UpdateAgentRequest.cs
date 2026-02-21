namespace Klacks.Api.Application.DTOs.Assistant;

public record UpdateAgentRequest(string? Name, string? DisplayName, string? Description, bool? IsActive);
