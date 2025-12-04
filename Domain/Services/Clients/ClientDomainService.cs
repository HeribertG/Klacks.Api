using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Clients;

public interface IClientDomainService
{
    Client CreateClient(string firstName, string name, GenderEnum gender, EntityTypeEnum type);
    void SoftDelete(Client client);
    void Restore(Client client);
    bool CanBeDeleted(Client client);
    string GetDisplayName(Client client);
}

public class ClientDomainService : IClientDomainService
{
    private readonly ILogger<ClientDomainService> _logger;
    private readonly List<IDomainEvent> _domainEvents = [];

    public ClientDomainService(ILogger<ClientDomainService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public Client CreateClient(string firstName, string name, GenderEnum gender, EntityTypeEnum type)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            Name = name,
            Gender = gender,
            Type = type,
            LegalEntity = gender == GenderEnum.LegalEntity
        };

        _logger.LogInformation("Created client {ClientId} with name {Name}",
            client.Id, GetDisplayName(client));

        _domainEvents.Add(new ClientCreatedEvent(client.Id, GetDisplayName(client)));

        return client;
    }

    public void SoftDelete(Client client)
    {
        if (!CanBeDeleted(client))
            throw new InvalidOperationException("Client cannot be deleted.");

        client.IsDeleted = true;
        client.DeletedTime = DateTime.UtcNow;

        _logger.LogInformation("Soft deleted client {ClientId}", client.Id);
    }

    public void Restore(Client client)
    {
        if (!client.IsDeleted)
            throw new InvalidOperationException("Client is not deleted.");

        client.IsDeleted = false;
        client.DeletedTime = null;

        _logger.LogInformation("Restored client {ClientId}", client.Id);
    }

    public bool CanBeDeleted(Client client)
    {
        return !client.IsDeleted;
    }

    public string GetDisplayName(Client client)
    {
        if (client.LegalEntity && !string.IsNullOrEmpty(client.Company))
            return client.Company;

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(client.FirstName))
            parts.Add(client.FirstName);

        if (!string.IsNullOrEmpty(client.Name))
            parts.Add(client.Name);

        return parts.Count > 0 ? string.Join(" ", parts) : $"Client {client.IdNumber}";
    }
}
