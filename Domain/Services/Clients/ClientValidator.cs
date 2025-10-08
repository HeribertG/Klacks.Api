using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientValidator : IClientValidator
{
    public void RemoveEmptyCollections(Client client)
    {
        for (int i = client.Annotations.Count - 1; i > -1; i--)
        {
            var itm = client.Annotations.ToList()[i];
            if (string.IsNullOrEmpty(itm.Note) || string.IsNullOrWhiteSpace(itm.Note))
            {
                client.Annotations.Remove(itm);
            }
        }

        for (int i = client.ClientContracts.Count - 1; i > -1; i--)
        {
            var itm = client.ClientContracts.ToList()[i];
            if (itm.ContractId == Guid.Empty)
            {
                client.ClientContracts.Remove(itm);
            }
        }

        for (int i = client.GroupItems.Count - 1; i > -1; i--)
        {
            var itm = client.GroupItems.ToList()[i];
            if (itm.GroupId == Guid.Empty)
            {
                client.GroupItems.Remove(itm);
            }
        }
    }

    public void EnsureSingleActiveContract(ICollection<ClientContract> clientContracts)
    {
        var activeContracts = clientContracts.Where(cc => cc.IsActive).ToList();

        if (activeContracts.Count > 1)
        {
            var lastActive = activeContracts.Last();

            foreach (var contract in clientContracts)
            {
                if (contract.Id != lastActive.Id)
                {
                    contract.IsActive = false;
                }
            }
        }
    }

    public void EnsureUniqueGroupItems(ICollection<GroupItem> groupItems)
    {
        var duplicateGroups = groupItems
            .GroupBy(gi => gi.GroupId)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var duplicateGroup in duplicateGroups)
        {
            var itemsToRemove = duplicateGroup.Take(duplicateGroup.Count() - 1).ToList();

            foreach (var item in itemsToRemove)
            {
                groupItems.Remove(item);
            }
        }
    }

    public void EnsureUniqueClientContracts(ICollection<ClientContract> clientContracts)
    {
        var duplicateContracts = clientContracts
            .GroupBy(cc => cc.ContractId)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var duplicateContract in duplicateContracts)
        {
            var itemsToRemove = duplicateContract.Take(duplicateContract.Count() - 1).ToList();

            foreach (var item in itemsToRemove)
            {
                clientContracts.Remove(item);
            }
        }
    }
}
