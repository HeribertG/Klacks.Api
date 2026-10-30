// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Authentification;

/// <summary>
/// The facts about one account that the user role change advisor needs, gathered by the caller and free of any
/// contact data.
/// </summary>
/// <param name="AccountId">Identifier of the account</param>
/// <param name="AccountName">Display name of the account; used in the sentences only</param>
/// <param name="CurrentRoles">Role names the account holds right now</param>
/// <param name="TargetRole">The role level to evaluate: Admin, Authorised or User</param>
/// <param name="AccountActive">False when the account is deactivated</param>
/// <param name="ActiveAdministratorCount">Number of active accounts that currently hold the Admin role</param>
/// <param name="IsCallerAccount">True when the account is the one that asks</param>
public sealed record UserRoleChangeFacts(
    string AccountId,
    string AccountName,
    IReadOnlyCollection<string> CurrentRoles,
    string TargetRole,
    bool AccountActive,
    int ActiveAdministratorCount,
    bool IsCallerAccount);
