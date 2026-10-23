// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

        var changes = ReadRequestedChanges(parameters);
        if (!changes.HasAnyValue())
        {
            return SkillResult.Error("Nothing to update. Provide at least one field to change.");
        }

        var portError = IdentityProviderSkillSupport.ValidatePort(changes.Port);
        if (portError != null)
        {
            return SkillResult.Error(portError);
        }

        var existing = await _mediator.Send(new GetQuery(id), cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error($"Identity provider '{id}' not found.");
        }

        var typeError = ApplyRequestedChanges(existing, changes);
        if (typeError != null)
        {
            return typeError;
        }

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
                    persisted => MatchesRequestedChanges(persisted, existing, changes),
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

    private static IdentityProviderChanges ReadRequestedChanges(Dictionary<string, object> parameters)
    {
        return new IdentityProviderChanges
        {
            Name = GetParameter<string>(parameters, "name"),
            TypeRaw = GetParameter<string>(parameters, "type"),
            IsEnabled = GetParameter<bool?>(parameters, "isEnabled"),
            SortOrder = GetParameter<int?>(parameters, "sortOrder"),
            UseForAuthentication = GetParameter<bool?>(parameters, "useForAuthentication"),
            UseForClientImport = GetParameter<bool?>(parameters, "useForClientImport"),
            Host = GetParameter<string>(parameters, "host"),
            Port = GetParameter<int?>(parameters, "port"),
            UseSsl = GetParameter<bool?>(parameters, "useSsl"),
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
    }

    private static SkillResult? ApplyRequestedChanges(IdentityProviderResource existing, IdentityProviderChanges changes)
    {
        if (!string.IsNullOrWhiteSpace(changes.TypeRaw))
        {
            var typeError = IdentityProviderSkillSupport.TryParseType(changes.TypeRaw, out var parsedType);
            if (typeError != null)
            {
                return SkillResult.Error(typeError);
            }

            existing.Type = parsedType;
        }

        if (!string.IsNullOrWhiteSpace(changes.Name))
        {
            existing.Name = changes.Name.Trim();
        }

        existing.IsEnabled = changes.IsEnabled ?? existing.IsEnabled;
        existing.SortOrder = changes.SortOrder ?? existing.SortOrder;
        existing.UseForAuthentication = changes.UseForAuthentication ?? existing.UseForAuthentication;
        existing.UseForClientImport = changes.UseForClientImport ?? existing.UseForClientImport;
        existing.Port = changes.Port ?? existing.Port;
        existing.UseSsl = changes.UseSsl ?? existing.UseSsl;
        existing.Host = Merge(changes.Host, existing.Host);
        existing.BaseDn = Merge(changes.BaseDn, existing.BaseDn);
        existing.BindDn = Merge(changes.BindDn, existing.BindDn);
        existing.BindPassword = Merge(changes.BindPassword, existing.BindPassword);
        existing.UserFilter = Merge(changes.UserFilter, existing.UserFilter);
        existing.ClientId = Merge(changes.ClientId, existing.ClientId);
        existing.ClientSecret = Merge(changes.ClientSecret, existing.ClientSecret);
        existing.AuthorizationUrl = Merge(changes.AuthorizationUrl, existing.AuthorizationUrl);
        existing.TokenUrl = Merge(changes.TokenUrl, existing.TokenUrl);
        existing.UserInfoUrl = Merge(changes.UserInfoUrl, existing.UserInfoUrl);
        existing.Scopes = Merge(changes.Scopes, existing.Scopes);
        existing.TenantId = Merge(changes.TenantId, existing.TenantId);
        return null;
    }

    private static string? Merge(string? provided, string? current)
    {
        return string.IsNullOrWhiteSpace(provided) ? current : provided.Trim();
    }

    private static bool MatchesRequestedChanges(
        IdentityProvider persisted,
        IdentityProviderResource intended,
        IdentityProviderChanges changes)
    {
        return (string.IsNullOrWhiteSpace(changes.Name) || persisted.Name == intended.Name)
            && (string.IsNullOrWhiteSpace(changes.TypeRaw) || persisted.Type == intended.Type)
            && (!changes.IsEnabled.HasValue || persisted.IsEnabled == intended.IsEnabled)
            && (!changes.SortOrder.HasValue || persisted.SortOrder == intended.SortOrder)
            && (!changes.UseForAuthentication.HasValue || persisted.UseForAuthentication == intended.UseForAuthentication)
            && (!changes.UseForClientImport.HasValue || persisted.UseForClientImport == intended.UseForClientImport)
            && (!changes.Port.HasValue || persisted.Port == intended.Port)
            && (!changes.UseSsl.HasValue || persisted.UseSsl == intended.UseSsl)
            && (string.IsNullOrWhiteSpace(changes.Host) || persisted.Host == intended.Host)
            && (string.IsNullOrWhiteSpace(changes.BaseDn) || persisted.BaseDn == intended.BaseDn)
            && (string.IsNullOrWhiteSpace(changes.BindDn) || persisted.BindDn == intended.BindDn)
            && (string.IsNullOrWhiteSpace(changes.BindPassword) || persisted.BindPassword == intended.BindPassword)
            && (string.IsNullOrWhiteSpace(changes.UserFilter) || persisted.UserFilter == intended.UserFilter)
            && (string.IsNullOrWhiteSpace(changes.ClientId) || persisted.ClientId == intended.ClientId)
            && (string.IsNullOrWhiteSpace(changes.ClientSecret) || persisted.ClientSecret == intended.ClientSecret)
            && (string.IsNullOrWhiteSpace(changes.AuthorizationUrl) || persisted.AuthorizationUrl == intended.AuthorizationUrl)
            && (string.IsNullOrWhiteSpace(changes.TokenUrl) || persisted.TokenUrl == intended.TokenUrl)
            && (string.IsNullOrWhiteSpace(changes.UserInfoUrl) || persisted.UserInfoUrl == intended.UserInfoUrl)
            && (string.IsNullOrWhiteSpace(changes.Scopes) || persisted.Scopes == intended.Scopes)
            && (string.IsNullOrWhiteSpace(changes.TenantId) || persisted.TenantId == intended.TenantId);
    }

    private sealed class IdentityProviderChanges
    {
        public string? Name { get; init; }
        public string? TypeRaw { get; init; }
        public bool? IsEnabled { get; init; }
        public int? SortOrder { get; init; }
        public bool? UseForAuthentication { get; init; }
        public bool? UseForClientImport { get; init; }
        public string? Host { get; init; }
        public int? Port { get; init; }
        public bool? UseSsl { get; init; }
        public string? BaseDn { get; init; }
        public string? BindDn { get; init; }
        public string? BindPassword { get; init; }
        public string? UserFilter { get; init; }
        public string? ClientId { get; init; }
        public string? ClientSecret { get; init; }
        public string? AuthorizationUrl { get; init; }
        public string? TokenUrl { get; init; }
        public string? UserInfoUrl { get; init; }
        public string? Scopes { get; init; }
        public string? TenantId { get; init; }

        public bool HasAnyValue()
        {
            return IsEnabled.HasValue
                   || SortOrder.HasValue
                   || UseForAuthentication.HasValue
                   || UseForClientImport.HasValue
                   || Port.HasValue
                   || UseSsl.HasValue
                   || new[]
                   {
                       Name, TypeRaw, Host, BaseDn, BindDn, BindPassword, UserFilter, ClientId,
                       ClientSecret, AuthorizationUrl, TokenUrl, UserInfoUrl, Scopes, TenantId
                   }.Any(value => !string.IsNullOrWhiteSpace(value));
        }
    }
}
