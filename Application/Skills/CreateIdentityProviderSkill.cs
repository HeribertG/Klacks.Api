// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new identity provider (LDAP, Active Directory, OAuth2 or OpenID Connect) via the
/// identity-provider PostCommand. Secret parameters (bindPassword, clientSecret) are accepted
/// but never echoed back — the result only carries boolean presence flags. The provider is
/// created disabled unless isEnabled is explicitly set to true.
/// </summary>
/// <param name="name">Required. Display name of the provider.</param>
/// <param name="type">Required. Ldap, ActiveDirectory, OAuth2 or OpenIdConnect.</param>
/// <param name="isEnabled">Optional. Defaults to false so the configuration can be reviewed before going live.</param>
/// <param name="sortOrder">Optional. Display/evaluation order; defaults to 0.</param>
/// <param name="useForAuthentication">Optional. Whether users can log in through this provider.</param>
/// <param name="useForClientImport">Optional. Whether clients are imported/synced from this provider.</param>
/// <param name="host">Optional. LDAP/AD server host name.</param>
/// <param name="port">Optional. LDAP/AD server port (1-65535).</param>
/// <param name="useSsl">Optional. Whether the LDAP/AD connection uses SSL.</param>
/// <param name="baseDn">Optional. LDAP base distinguished name.</param>
/// <param name="bindDn">Optional. LDAP bind distinguished name.</param>
/// <param name="bindPassword">Optional secret. LDAP bind password; masked in all responses.</param>
/// <param name="userFilter">Optional. LDAP user filter expression.</param>
/// <param name="clientId">Optional. OAuth2/OIDC client id.</param>
/// <param name="clientSecret">Optional secret. OAuth2/OIDC client secret; masked in all responses.</param>
/// <param name="authorizationUrl">Optional. OAuth2/OIDC authorization endpoint.</param>
/// <param name="tokenUrl">Optional. OAuth2/OIDC token endpoint.</param>
/// <param name="userInfoUrl">Optional. OAuth2/OIDC user-info endpoint.</param>
/// <param name="scopes">Optional. OAuth2/OIDC scopes (space-separated).</param>
/// <param name="tenantId">Optional. Tenant id for multi-tenant providers (e.g. Entra ID).</param>

using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_identity_provider")]
public class CreateIdentityProviderSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateIdentityProviderSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("Missing required parameter 'name'.");
        }

        var typeRaw = GetParameter<string>(parameters, "type");
        if (string.IsNullOrWhiteSpace(typeRaw))
        {
            return SkillResult.Error($"Missing required parameter 'type'. Valid values: {string.Join(", ", Enum.GetNames<IdentityProviderType>())}.");
        }

        var typeError = IdentityProviderSkillSupport.TryParseType(typeRaw, out var type);
        if (typeError != null)
        {
            return SkillResult.Error(typeError);
        }

        var port = GetParameter<int?>(parameters, "port");
        var portError = IdentityProviderSkillSupport.ValidatePort(port);
        if (portError != null)
        {
            return SkillResult.Error(portError);
        }

        var resource = new IdentityProviderResource
        {
            Name = name.Trim(),
            Type = type,
            IsEnabled = GetParameter<bool?>(parameters, "isEnabled") ?? false,
            SortOrder = GetParameter<int?>(parameters, "sortOrder") ?? 0,
            UseForAuthentication = GetParameter<bool?>(parameters, "useForAuthentication") ?? false,
            UseForClientImport = GetParameter<bool?>(parameters, "useForClientImport") ?? false,
            Host = GetParameter<string>(parameters, "host"),
            Port = port,
            UseSsl = GetParameter<bool?>(parameters, "useSsl") ?? false,
            BaseDn = GetParameter<string>(parameters, "baseDn"),
            BindDn = GetParameter<string>(parameters, "bindDn"),
            BindPassword = GetParameter<string>(parameters, "bindPassword"),
            UserFilter = GetParameter<string>(parameters, "userFilter"),
            ClientId = GetParameter<string>(parameters, "clientId"),
            ClientSecret = GetParameter<string>(parameters, "clientSecret"),
            AuthorizationUrl = GetParameter<string>(parameters, "authorizationUrl"),
            TokenUrl = GetParameter<string>(parameters, "tokenUrl"),
            UserInfoUrl = GetParameter<string>(parameters, "userInfoUrl"),
            Scopes = GetParameter<string>(parameters, "scopes"),
            TenantId = GetParameter<string>(parameters, "tenantId")
        };

        var created = await _mediator.Send(new PostCommand(resource), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error($"Identity provider '{resource.Name}' could not be created.");
        }

        var enabledNote = created.IsEnabled
            ? "enabled"
            : "disabled — enable it via update_identity_provider once the configuration is verified";

        return SkillResult.SuccessResult(
            IdentityProviderSkillSupport.ToMaskedProjection(created),
            $"Identity provider '{created.Name}' ({created.Type}) created (id {created.Id}), {enabledNote}. Secrets are stored but never returned.");
    }
}
