using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs.IdentityProviders;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class IdentityProviderMapper
{
    public partial IdentityProviderResource ToResource(IdentityProvider entity);
    public partial List<IdentityProviderResource> ToResources(List<IdentityProvider> entities);

    public partial IdentityProviderListResource ToListResource(IdentityProvider entity);
    public partial List<IdentityProviderListResource> ToListResources(List<IdentityProvider> entities);

    [MapperIgnoreTarget(nameof(IdentityProvider.CreateTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(IdentityProvider.UpdateTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(IdentityProvider.DeletedTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.IsDeleted))]
    [MapperIgnoreTarget(nameof(IdentityProvider.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(IdentityProvider.LastSyncTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.LastSyncCount))]
    [MapperIgnoreTarget(nameof(IdentityProvider.LastSyncError))]
    public partial IdentityProvider ToEntity(IdentityProviderResource resource);

    [MapperIgnoreTarget(nameof(IdentityProvider.Id))]
    [MapperIgnoreTarget(nameof(IdentityProvider.CreateTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(IdentityProvider.UpdateTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(IdentityProvider.DeletedTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.IsDeleted))]
    [MapperIgnoreTarget(nameof(IdentityProvider.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(IdentityProvider.LastSyncTime))]
    [MapperIgnoreTarget(nameof(IdentityProvider.LastSyncCount))]
    [MapperIgnoreTarget(nameof(IdentityProvider.LastSyncError))]
    public partial void UpdateEntity(IdentityProviderResource resource, IdentityProvider target);
}
