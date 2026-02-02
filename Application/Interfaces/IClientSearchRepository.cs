using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientSearchRepository
{
    Task<Client?> FindByMail(string mail);

    Task<List<Client>> FindList(string? company = null, string? name = null, string? firstname = null);

    Task<string> FindStatePostCode(string zip);

    Task<List<ClientForReplacementResource>> GetClientsForReplacement();

    Task<ClientSearchResult> SearchAsync(
        string? searchTerm = null,
        string? canton = null,
        EntityTypeEnum? entityType = null,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

public record ClientSearchResult
{
    public required IReadOnlyList<ClientSearchItem> Items { get; init; }
    public int TotalCount { get; init; }
}

public record ClientSearchItem
{
    public Guid Id { get; init; }
    public int? IdNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Company { get; init; }
    public string? Gender { get; init; }
    public string? EntityType { get; init; }
    public string? Canton { get; init; }
    public string? City { get; init; }
}