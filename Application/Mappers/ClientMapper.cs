using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Clients;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Presentation.DTOs.Staffs;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class ClientMapper
{
    public partial ClientListItemResource ToListItemResource(Client client);
    public partial List<ClientListItemResource> ToListItemResources(List<Client> clients);

    public TruncatedClientResource ToTruncatedResource(TruncatedClient truncated)
    {
        return new TruncatedClientResource
        {
            Clients = truncated.Clients?.Select(ToListItemResource).ToList(),
            Editor = truncated.Editor,
            LastChange = truncated.LastChange,
            MaxItems = truncated.MaxItems,
            MaxPages = truncated.MaxPages,
            CurrentPage = truncated.CurrentPage,
            FirstItemOnPage = truncated.FirstItemOnPage
        };
    }

    [MapperIgnoreTarget(nameof(Client.CreateTime))]
    [MapperIgnoreTarget(nameof(Client.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Client.UpdateTime))]
    [MapperIgnoreTarget(nameof(Client.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Client.DeletedTime))]
    [MapperIgnoreTarget(nameof(Client.IsDeleted))]
    [MapperIgnoreTarget(nameof(Client.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Client.Breaks))]
    public partial Client ToEntity(ClientResource resource);

    public ClientResource ToResource(Client client)
    {
        if (client == null) return null!;

        return new ClientResource
        {
            Id = client.Id,
            Name = client.Name,
            FirstName = client.FirstName,
            SecondName = client.SecondName,
            MaidenName = client.MaidenName,
            Title = client.Title,
            Company = client.Company,
            Gender = client.Gender,
            IdNumber = client.IdNumber,
            LegalEntity = client.LegalEntity,
            Birthdate = client.Birthdate,
            PasswortResetToken = client.PasswortResetToken,
            IsDeleted = client.IsDeleted,
            Type = (int)client.Type,
            Addresses = client.Addresses?.Select(ToAddressResource).ToList() ?? new List<AddressResource>(),
            Communications = client.Communications?.Select(ToCommunicationResource).ToList() ?? new List<CommunicationResource>(),
            Annotations = client.Annotations?.OrderByDescending(a => a.CreateTime).Select(ToAnnotationResource).ToList() ?? new List<AnnotationResource>(),
            Works = client.Works?.Select(ToWorkResource).ToList() ?? new List<WorkResource>(),
            ClientContracts = client.ClientContracts?.Select(ToContractResource).ToList() ?? new List<ClientContractResource>(),
            GroupItems = client.GroupItems?.Select(ToGroupItemResource).ToList() ?? new List<ClientGroupItemResource>(),
            Membership = client.Membership != null ? ToMembershipResource(client.Membership) : null,
            ClientImage = client.ClientImage != null ? ToImageResource(client.ClientImage) : null
        };
    }

    public partial AnnotationResource ToAnnotationResource(Annotation annotation);
    public partial AddressResource ToAddressResource(Address address);
    public partial CommunicationResource ToCommunicationResource(Communication communication);
    public partial WorkResource ToWorkResource(Work work);
    public partial MembershipResource ToMembershipResource(Membership membership);

    public ClientImageResource ToImageResource(ClientImage image)
    {
        return new ClientImageResource
        {
            Id = image.Id,
            ClientId = image.ClientId,
            ImageData = image.ImageData != null && image.ImageData.Length > 0
                ? Convert.ToBase64String(image.ImageData)
                : string.Empty,
            ContentType = image.ContentType,
            FileName = image.FileName,
            FileSize = image.FileSize
        };
    }

    public ClientImage ToImageEntity(ClientImageResource resource)
    {
        return new ClientImage
        {
            ClientId = resource.ClientId,
            ImageData = !string.IsNullOrEmpty(resource.ImageData)
                ? Convert.FromBase64String(resource.ImageData)
                : [],
            ContentType = resource.ContentType,
            FileName = resource.FileName,
            FileSize = resource.FileSize
        };
    }

    [MapperIgnoreSource(nameof(ClientContract.Client))]
    [MapperIgnoreSource(nameof(ClientContract.Contract))]
    public partial ClientContractResource ToContractResource(ClientContract contract);
    public partial List<ClientContractResource> ToContractResources(ICollection<ClientContract> contracts);

    [MapperIgnoreTarget(nameof(ClientContract.CreateTime))]
    [MapperIgnoreTarget(nameof(ClientContract.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ClientContract.UpdateTime))]
    [MapperIgnoreTarget(nameof(ClientContract.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ClientContract.DeletedTime))]
    [MapperIgnoreTarget(nameof(ClientContract.IsDeleted))]
    [MapperIgnoreTarget(nameof(ClientContract.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ClientContract.Client))]
    [MapperIgnoreTarget(nameof(ClientContract.Contract))]
    public partial ClientContract ToContractEntity(ClientContractResource resource);

    public partial ClientBreakResource ToBreakResource(Client client);

    [MapperIgnoreTarget(nameof(ClientWorkResource.Membership))]
    [MapperIgnoreTarget(nameof(ClientWorkResource.NeededRows))]
    public partial ClientWorkResource ToWorkResource(Client client);

    public ClientGroupItemResource ToGroupItemResource(GroupItem groupItem)
    {
        return new ClientGroupItemResource
        {
            GroupId = groupItem.GroupId,
            ClientId = groupItem.ClientId,
            GroupName = groupItem.Group?.Name ?? string.Empty,
            Description = groupItem.Group?.Description ?? string.Empty,
            ValidFrom = groupItem.ValidFrom,
            ValidUntil = groupItem.ValidUntil
        };
    }

    public List<ClientGroupItemResource> ToGroupItemResources(ICollection<GroupItem> groupItems)
    {
        return groupItems.Select(ToGroupItemResource).ToList();
    }

    [MapperIgnoreTarget(nameof(GroupItem.Id))]
    [MapperIgnoreTarget(nameof(GroupItem.ShiftId))]
    [MapperIgnoreTarget(nameof(GroupItem.Client))]
    [MapperIgnoreTarget(nameof(GroupItem.Shift))]
    [MapperIgnoreTarget(nameof(GroupItem.Group))]
    [MapperIgnoreTarget(nameof(GroupItem.CreateTime))]
    [MapperIgnoreTarget(nameof(GroupItem.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(GroupItem.UpdateTime))]
    [MapperIgnoreTarget(nameof(GroupItem.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(GroupItem.DeletedTime))]
    [MapperIgnoreTarget(nameof(GroupItem.IsDeleted))]
    [MapperIgnoreTarget(nameof(GroupItem.CurrentUserDeleted))]
    public partial GroupItem ToGroupItemEntity(ClientGroupItemResource resource);

    public TruncatedClient ToTruncatedClient(PagedResult<Client> pagedResult)
    {
        return new TruncatedClient
        {
            Clients = pagedResult.Items.ToList(),
            MaxItems = pagedResult.TotalCount,
            MaxPages = pagedResult.TotalPages,
            CurrentPage = pagedResult.PageNumber,
            FirstItemOnPage = pagedResult.Items.Any() ? pagedResult.PageNumber * pagedResult.PageSize : -1
        };
    }

    public ClientResource FromSummary(ClientSummary summary)
    {
        return new ClientResource
        {
            Id = Guid.NewGuid(),
            FirstName = summary.FirstName,
            Name = summary.LastName,
            Company = summary.Company,
            Gender = summary.Gender ?? GenderEnum.Female,
            IdNumber = ParseIdNumber(summary.IdNumber),
            Birthdate = summary.DateOfBirth.HasValue
                ? summary.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            LegalEntity = summary.Gender == GenderEnum.LegalEntity,
            IsDeleted = !summary.IsActive
        };
    }

    private static int ParseIdNumber(string idNumber)
    {
        return int.TryParse(idNumber, out var id) ? id : 0;
    }
}
