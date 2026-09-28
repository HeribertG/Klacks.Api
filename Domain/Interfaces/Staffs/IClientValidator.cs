// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IClientValidator
{
    void RemoveEmptyCollections(Client client);
    void EnsureSingleActiveContract(ICollection<ClientContract> clientContracts);
    void EnsureUniqueGroupItems(ICollection<GroupItem> groupItems);
    void EnsureUniqueClientContracts(ICollection<ClientContract> clientContracts);
    void EnsureUniqueQualifications(ICollection<ClientQualification> qualifications);
}
