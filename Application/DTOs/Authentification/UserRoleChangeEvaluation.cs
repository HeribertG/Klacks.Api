// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Authentification;

/// <summary>
/// Outcome of advising on a role change for one account. The facts (roles, rights, administrator count) are kept
/// apart from the findings and the recommendation, which are computed deterministically; nothing is changed.
/// </summary>
/// <param name="AccountId">Identifier of the evaluated account</param>
/// <param name="AccountName">Display name of the evaluated account</param>
/// <param name="CurrentRole">Highest role the account holds now: Admin, Authorised or User</param>
/// <param name="TargetRole">The evaluated role level</param>
/// <param name="AccountActive">False when the account is deactivated</param>
/// <param name="ActiveAdministratorCount">Number of active accounts that currently hold the Admin role</param>
/// <param name="RightsGained">Rights the account would gain</param>
/// <param name="RightsLost">Rights the account would lose</param>
/// <param name="Findings">Deterministic findings, most important first</param>
/// <param name="Recommendation">Conservative summary of what the change means</param>
public sealed record UserRoleChangeEvaluation(
    string AccountId,
    string AccountName,
    string CurrentRole,
    string TargetRole,
    bool AccountActive,
    int ActiveAdministratorCount,
    IReadOnlyList<string> RightsGained,
    IReadOnlyList<string> RightsLost,
    IReadOnlyList<UserRoleChangeFinding> Findings,
    string Recommendation);
