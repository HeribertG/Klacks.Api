using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Presentation.DTOs.Staffs;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class AddressCommunicationMapper
{
    public partial AddressResource ToAddressResource(Address address);
    public partial List<AddressResource> ToAddressResources(List<Address> addresses);

    [MapperIgnoreTarget(nameof(Address.CreateTime))]
    [MapperIgnoreTarget(nameof(Address.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Address.UpdateTime))]
    [MapperIgnoreTarget(nameof(Address.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Address.DeletedTime))]
    [MapperIgnoreTarget(nameof(Address.IsDeleted))]
    [MapperIgnoreTarget(nameof(Address.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Address.Client))]
    public partial Address ToAddressEntity(AddressResource resource);

    public partial CommunicationResource ToCommunicationResource(Communication communication);
    public partial List<CommunicationResource> ToCommunicationResources(List<Communication> communications);

    [MapperIgnoreTarget(nameof(Communication.CreateTime))]
    [MapperIgnoreTarget(nameof(Communication.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Communication.UpdateTime))]
    [MapperIgnoreTarget(nameof(Communication.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Communication.DeletedTime))]
    [MapperIgnoreTarget(nameof(Communication.IsDeleted))]
    [MapperIgnoreTarget(nameof(Communication.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Communication.Client))]
    public partial Communication ToCommunicationEntity(CommunicationResource resource);

    public partial CommunicationTypeResource ToCommunicationTypeResource(CommunicationType type);
    public partial List<CommunicationTypeResource> ToCommunicationTypeResources(List<CommunicationType> types);

    public partial CommunicationType ToCommunicationTypeEntity(CommunicationTypeResource resource);
}
