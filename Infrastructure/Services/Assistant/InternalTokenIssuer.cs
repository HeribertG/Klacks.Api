// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mints short-lived tokens so background work — scheduled tasks, goal plans, the messenger bridges —
/// reaches the own REST API under a real identity instead of a permission list frozen at authoring
/// time. Reading the owner's roles fresh on every mint is the point: revoking a role takes effect on the
/// next run rather than whenever someone remembers to re-create the schedule. Refuses when the owner is
/// gone, locked out or holds no role at all — the last case matters because a role-less token would be
/// rejected by the assistant policy at the endpoint anyway, and a readable refusal beats a bare 403.
/// </summary>
/// <param name="userManager">Resolves the owner and their current roles and lockout state</param>
/// <param name="tokenService">Signs the token with the same key and settings as a login token</param>
/// <param name="logger">Records refusals, which are security-relevant events</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class InternalTokenIssuer : IInternalTokenIssuer
{
    /// <summary>Long enough for one background run, short enough that a leaked token ages out fast.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private const string TokenUseClaimType = "token_use";
    private const string InternalTokenUse = "internal";

    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<InternalTokenIssuer> _logger;

    public InternalTokenIssuer(
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        ILogger<InternalTokenIssuer> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<InternalTokenResult> IssueForOwnerAsync(
        Guid ownerUserId,
        string? capToRole = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(ownerUserId.ToString());
        if (user is null)
        {
            return Refuse(ownerUserId, "the owner account no longer exists");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Refuse(ownerUserId, "the owner account is locked out");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return Refuse(ownerUserId, "the owner account holds no role");
        }

        var effectiveRoles = ApplyCeiling(roles, capToRole);

        var jwt = await _tokenService.CreateToken(
            user,
            DateTime.UtcNow.Add(Lifetime),
            effectiveRoles,
            new Dictionary<string, string> { [TokenUseClaimType] = InternalTokenUse });

        return InternalTokenResult.Issued(new BearerToken(jwt), effectiveRoles);
    }

    /// <summary>
    /// Caps the role set at <paramref name="capToRole"/> without ever adding rights: Admin collapses to
    /// the ceiling, anything at or below it is left alone. Mirrors McpPrincipalCapper, which does the
    /// same for an already-authenticated principal.
    /// </summary>
    private static IReadOnlyList<string> ApplyCeiling(IList<string> roles, string? capToRole)
    {
        if (string.IsNullOrWhiteSpace(capToRole) || !roles.Contains(Roles.Admin))
        {
            return roles.ToList();
        }

        var capped = roles.Where(role => role != Roles.Admin).ToList();
        if (!capped.Contains(capToRole))
        {
            capped.Add(capToRole);
        }

        return capped;
    }

    private InternalTokenResult Refuse(Guid ownerUserId, string reason)
    {
        _logger.LogWarning(
            "Refused to issue an internal token for owner {OwnerUserId}: {Reason}", ownerUserId, reason);

        return InternalTokenResult.Refused(
            $"This could not run because {reason}. Ask an administrator to check the account.");
    }
}
