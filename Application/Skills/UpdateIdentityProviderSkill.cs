// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing identity provider. Loads the current configuration via GetQuery, merges
/// only the supplied parameters onto it and dispatches the identity-provider PutCommand inside a
/// transaction that re-reads the row afterwards and rolls back if any supplied field does not
/// match. Secret parameters (bindPassword, clientSecret) replace the stored value only when
/// supplied and are never echoed back — responses carry boolean presence flags instead.
/// </summary>
/// <param name="id">Required. UUID of the identity provider to update (find it via list_identity_providers).</param>
/// <param name="name">Optional. New display name.</param>
/// <param name="type">Optional. Ldap, ActiveDirectory, OAuth2 or OpenIdConnect.</param>
/// <param name="isEnabled">Optional. Enables or disables the provider.</param>
/// <param name="sortOrder">Optional. New display/evaluation order.</param>
/// <param name="useForAuthentication">Optional. Whether users can log in through this provider.</param>
/// <param name="useForClientImport">Optional. Whether clients are imported/synced from this provider.</param>
/// <param name="host">Optional. LDAP/AD server host name.</param>
/// <param name="port">Optional. LDAP/AD server port (1-65535).</param>
/// <param name="useSsl">Optional. Whether the LDAP/AD connection uses SSL.</param>
/// <param name="baseDn">Optional. LDAP base distinguished name.</param>
/// <param name="bindDn">Optional. LDAP bind distinguished name.</param>
/// <param name="bindPassword">Optional secret. New LDAP bind password; masked in all responses.</param>
/// <param name="userFilter">Optional. LDAP user filter expression.</param>
/// <param name="clientId">Optional. OAuth2/OIDC client id.</param>
/// <param name="clientSecret">Optional secret. New OAuth2/OIDC client secret; masked in all responses.</param>
/// <param name="authorizationUrl">Optional. OAuth2/OIDC authorization endpoint.</param>
/// <param name="tokenUrl">Optional. OAuth2/OIDC token endpoint.</param>
/// <param name="userInfoUrl">Optional. OAuth2/OIDC user-info endpoint.</param>
/// <param name="scopes">Optional. OAuth2/OIDC scopes (space-separated).</param>
/// <param name="tenantId">Optional. Tenant id for multi-tenant providers.</param>

using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.IdentityProviders;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_identity_provider")]
public class UpdateIdentityProviderSkill : BaseSkillImplementation
{
    private const string SkillName = "update_identity_provider";

    private readonly IMediator _mediator;
    private readonly IIdentityProviderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateIdentityProviderSkill(
        IMediator mediator,
        IIdentityProviderRepository repository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var idRaw = GetParameter<string>(parameters, "id");
        if (string.IsNullOrWhiteSpace(idRaw) || !Guid.TryParse(idRaw, out var id))
        {
            return SkillResult.Error("Missing or invalid required parameter 'id'. Use list_identity_providers to find the provider id.");
        }

        var name = GetParameter<string>(parameters, "name");
        var typeRaw = GetParameter<string>(parameters, "type");
        var isEnabled = GetParameter<bool?>(parameters, "isEnabled");
        var sortOrder = GetParameter<int?>(parameters, "sortOrder");
        var useForAuthentication = GetParameter<bool?>(parameters, "useForAuthentication");
        var useForClientImport = GetParameter<bool?>(parameters, "useForClientImport");
        var host = GetParameter<string>(parameters, "host");
        var port = GetParameter<int?>(parameters, "port");
        var useSsl = GetParameter<bool?>(parameters, "useSsl");
        var baseDn = GetParameter<string>(parameters, "baseDn");
        var bindDn = GetParameter<string>(parameters, "bindDn");
        var bindPassword = GetParameter<string>(parameters, "bindPassword");
        var userFilter = GetParameter<string>(parameters, "userFilter");
        var clientId = GetParameter<string>(parameters, "clientId");
        var clientSecret = GetParameter<string>(parameters, "clientSecret");
        var authorizationUrl = GetParameter<string>(parameters, "authorizationUrl");
        var tokenUrl = GetParameter<string>(parameters, "tokenUrl");
        var userInfoUrl = GetParameter<string>(parameters, "userInfoUrl");
        var scopes = GetParameter<string>(parameters, "scopes");
        var tenantId = GetParameter<string>(parameters, "tenantId");

        var hasUpdate = isEnabled.HasValue
                        || sortOrder.HasValue
                        || useForAuthentication.HasValue
                        || useForClientImport.HasValue
                        || port.HasValue
                        || useSsl.HasValue
                        || new[]
                        {
                            name, typeRaw, host, baseDn, bindDn, bindPassword, userFilter, clientId,
                            clientSecret, authorizationUrl, tokenUrl, userInfoUrl, scopes, tenantId
                        }.Any(value => !string.IsNullOrWhiteSpace(value));
        if (!hasUpdate)
        {
            return SkillResult.Error("Nothing to update. Provide at least one field to change.");
        }

        var portError = IdentityProviderSkillSupport.ValidatePort(port);
        if (portError != null)
        {
            return SkillResult.Error(portError);
        }

        var existing = await _mediator.Send(new GetQuery(id), cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error($"Identity provider '{id}' not found.");
        }

        if (!string.IsNullOrWhiteSpace(typeRaw))
        {
            var typeError = IdentityProviderSkillSupport.TryParseType(typeRaw, out var parsedType);
            if (typeError != null)
            {
                return SkillResult.Error(typeError);
            }

            existing.Type = parsedType;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            existing.Name = name.Trim();
        }

        existing.IsEnabled = isEnabled ?? existing.IsEnabled;
        existing.SortOrder = sortOrder ?? existing.SortOrder;
        existing.UseForAuthentication = useForAuthentication ?? existing.UseForAuthentication;
        existing.UseForClientImport = useForClientImport ?? existing.UseForClientImport;
        existing.Port = port ?? existing.Port;
        existing.UseSsl = useSsl ?? existing.UseSsl;
        existing.Host = Merge(host, existing.Host);
        existing.BaseDn = Merge(baseDn, existing.BaseDn);
        existing.BindDn = Merge(bindDn, existing.BindDn);
        existing.BindPassword = Merge(bindPassword, existing.BindPassword);
        existing.UserFilter = Merge(userFilter, existing.UserFilter);
        existing.ClientId = Merge(clientId, existing.ClientId);
        existing.ClientSecret = Merge(clientSecret, existing.ClientSecret);
        existing.AuthorizationUrl = Merge(authorizationUrl, existing.AuthorizationUrl);
        existing.TokenUrl = Merge(tokenUrl, existing.TokenUrl);
        existing.UserInfoUrl = Merge(userInfoUrl, existing.UserInfoUrl);
        existing.Scopes = Merge(scopes, existing.Scopes);
        existing.TenantId = Merge(tenantId, existing.TenantId);

        IdentityProviderResource? updated = null;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                updated = await _mediator.Send(new PutCommand(existing), cancellationToken);
                if (updated == null)
                {
                    throw new SkillVerificationException(SkillName, $"Identity provider '{id}' could not be updated.");
                }

                await ConfirmPersistedAsync(
                    SkillName,
                    () => _repository.GetNoTracking(id),
                    persisted => MatchesRequestedChanges(
                        persisted, existing,
                        name, typeRaw, isEnabled, sortOrder, useForAuthentication, useForClientImport,
                        host, port, useSsl, baseDn, bindDn, bindPassword, userFilter,
                        clientId, clientSecret, authorizationUrl, tokenUrl, userInfoUrl, scopes, tenantId),
                    $"the identity provider '{existing.Name}' update");

                return updated.Id;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            IdentityProviderSkillSupport.ToMaskedProjection(updated!),
            $"Identity provider '{updated!.Name}' ({updated.Type}) updated (id {updated.Id}) and confirmed in the database (verified). Secrets are stored but never returned.");
    }

    private static string? Merge(string? provided, string? current)
    {
        return string.IsNullOrWhiteSpace(provided) ? current : provided.Trim();
    }

    private static bool MatchesRequestedChanges(
        IdentityProvider persisted,
        IdentityProviderResource intended,
        string? name, string? typeRaw, bool? isEnabled, int? sortOrder,
        bool? useForAuthentication, bool? useForClientImport,
        string? host, int? port, bool? useSsl,
        string? baseDn, string? bindDn, string? bindPassword, string? userFilter,
        string? clientId, string? clientSecret, string? authorizationUrl,
        string? tokenUrl, string? userInfoUrl, string? scopes, string? tenantId)
    {
        return (string.IsNullOrWhiteSpace(name) || persisted.Name == intended.Name)
            && (string.IsNullOrWhiteSpace(typeRaw) || persisted.Type == intended.Type)
            && (!isEnabled.HasValue || persisted.IsEnabled == intended.IsEnabled)
            && (!sortOrder.HasValue || persisted.SortOrder == intended.SortOrder)
            && (!useForAuthentication.HasValue || persisted.UseForAuthentication == intended.UseForAuthentication)
            && (!useForClientImport.HasValue || persisted.UseForClientImport == intended.UseForClientImport)
            && (!port.HasValue || persisted.Port == intended.Port)
            && (!useSsl.HasValue || persisted.UseSsl == intended.UseSsl)
            && (string.IsNullOrWhiteSpace(host) || persisted.Host == intended.Host)
            && (string.IsNullOrWhiteSpace(baseDn) || persisted.BaseDn == intended.BaseDn)
            && (string.IsNullOrWhiteSpace(bindDn) || persisted.BindDn == intended.BindDn)
            && (string.IsNullOrWhiteSpace(bindPassword) || persisted.BindPassword == intended.BindPassword)
            && (string.IsNullOrWhiteSpace(userFilter) || persisted.UserFilter == intended.UserFilter)
            && (string.IsNullOrWhiteSpace(clientId) || persisted.ClientId == intended.ClientId)
            && (string.IsNullOrWhiteSpace(clientSecret) || persisted.ClientSecret == intended.ClientSecret)
            && (string.IsNullOrWhiteSpace(authorizationUrl) || persisted.AuthorizationUrl == intended.AuthorizationUrl)
            && (string.IsNullOrWhiteSpace(tokenUrl) || persisted.TokenUrl == intended.TokenUrl)
            && (string.IsNullOrWhiteSpace(userInfoUrl) || persisted.UserInfoUrl == intended.UserInfoUrl)
            && (string.IsNullOrWhiteSpace(scopes) || persisted.Scopes == intended.Scopes)
            && (string.IsNullOrWhiteSpace(tenantId) || persisted.TenantId == intended.TenantId);
    }
}
