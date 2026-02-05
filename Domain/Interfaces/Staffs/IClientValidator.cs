using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IClientValidator
{
    void RemoveEmptyCollections(Client client);
    void EnsureSingleActiveContract(ICollection<ClientContract> clientContracts);
    void EnsureUniqueGroupItems(ICollection<GroupItem> groupItems);
    void EnsureUniqueClientContracts(ICollection<ClientContract> clientContracts);
}
