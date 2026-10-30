// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on giving one account the role Admin, Authorised or User, derived only from the real
/// permission model: the rights gained and lost (including the Planer floor every caller keeps), whether settings
/// access appears or disappears, the Admin bypass, an unchanged role, a deactivated account, the caller cutting off
/// their own settings access and the demotion of the last active administrator. Pure and static, so every rule is
/// pinned by a unit test; the model only relays it.
/// </summary>
/// <param name="facts">The gathered facts about the account and the requested role</param>

using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Authentication;

public static class UserRoleChangeAdvisor
{
    private const string RightSeparator = ", ";
    private const string ConfirmationNotice = "Nothing has been changed; the administrator decides, and the system asks for one confirmation before the role is changed.";

    public static UserRoleChangeEvaluation Evaluate(UserRoleChangeFacts facts)
    {
        var currentRole = EffectiveRole(facts.CurrentRoles);
        var currentRights = RightsOf(facts.CurrentRoles);
        var targetRights = RightsOf(RolesToHold(facts.TargetRole));
        var gained = targetRights.Except(currentRights).ToList();
        var lost = currentRights.Except(targetRights).ToList();
        var findings = Analyse(facts, currentRole, gained, lost);

        return new UserRoleChangeEvaluation(
            facts.AccountId,
            facts.AccountName,
            currentRole,
            facts.TargetRole,
            facts.AccountActive,
            facts.ActiveAdministratorCount,
            gained,
            lost,
            findings,
            BuildRecommendation(facts, currentRole, findings, gained.Count, lost.Count));
    }

    public static string EffectiveRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(Roles.Admin))
        {
            return Roles.Admin;
        }

        return roles.Contains(Roles.Authorised) ? Roles.Authorised : Roles.User;
    }

    private static List<UserRoleChangeFinding> Analyse(
        UserRoleChangeFacts facts, string currentRole, IReadOnlyList<string> gained, IReadOnlyList<string> lost)
    {
        var findings = new List<UserRoleChangeFinding>();

        if (currentRole == facts.TargetRole)
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.RoleUnchanged,
                $"{facts.AccountName} already holds the role {currentRole}; the rights stay as they are.",
                false));
        }
        else
        {
            AddRoleChangeFindings(facts, currentRole, gained, lost, findings);
        }

        if (!facts.AccountActive)
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.AccountDeactivated,
                $"The account of {facts.AccountName} is deactivated; a role change does not reactivate it.",
                false));
        }

        return findings;
    }

    private static void AddRoleChangeFindings(
        UserRoleChangeFacts facts, string currentRole, IReadOnlyList<string> gained, IReadOnlyList<string> lost, List<UserRoleChangeFinding> findings)
    {
        var name = facts.AccountName;
        var losesAdmin = currentRole == Roles.Admin;

        if (LastAdministratorRule.WouldRemoveLastActiveAdministrator(losesAdmin, facts.AccountActive, facts.ActiveAdministratorCount))
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.LastAdministratorDemotion,
                $"{name} is the only active administrator; setting the role to {facts.TargetRole} would leave the installation without an active administrator.",
                true));
        }

        if (facts.IsCallerAccount && lost.Contains(Permissions.CanEditSettings))
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.OwnAccountLosesSettingsAccess,
                "This is your own account: after the change you could no longer edit the settings or give the role back yourself.",
                false));
        }

        if (losesAdmin)
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.AdminBypassLost,
                $"{name} would lose the Admin role, which passes every permission check; from then on only the individual rights count.",
                false));
        }
        else if (facts.TargetRole == Roles.Admin)
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.AdminBypassGained,
                $"{name} would gain the Admin role, which passes every permission check, including user administration.",
                false));
        }

        AddSettingsFinding(UserRoleChangeFindingCode.SettingsAccessLost, $"{name} would lose access to the settings", lost, findings);
        AddSettingsFinding(UserRoleChangeFindingCode.SettingsAccessGained, $"{name} would gain access to the settings", gained, findings);

        if (lost.Count > 0)
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.RightsLost,
                $"{name} would lose {lost.Count} right(s): {Join(lost)}.",
                false));
        }

        if (gained.Count > 0)
        {
            findings.Add(new UserRoleChangeFinding(
                UserRoleChangeFindingCode.RightsGained,
                $"{name} would gain {gained.Count} right(s): {Join(gained)}.",
                false));
        }
    }

    private static void AddSettingsFinding(
        UserRoleChangeFindingCode code, string opening, IReadOnlyList<string> rights, List<UserRoleChangeFinding> findings)
    {
        var settingsRights = rights.Where(IsSettingsRight).ToList();
        if (settingsRights.Count > 0)
        {
            findings.Add(new UserRoleChangeFinding(code, $"{opening} ({Join(settingsRights)}).", false));
        }
    }

    private static string BuildRecommendation(
        UserRoleChangeFacts facts, string currentRole, IReadOnlyList<UserRoleChangeFinding> findings, int gainedCount, int lostCount)
    {
        var blocking = findings.FirstOrDefault(finding => finding.IsBlocking);
        if (blocking != null)
        {
            return $"Do not set {facts.AccountName} to {facts.TargetRole}: {blocking.Statement} {ConfirmationNotice}";
        }

        if (currentRole == facts.TargetRole)
        {
            return $"{facts.AccountName} already holds the role {currentRole}; nothing would change. {ConfirmationNotice}";
        }

        var more = findings.Count - 1;
        var moreText = more > 0 ? $" {more} further finding(s) are listed." : string.Empty;
        return $"Setting {facts.AccountName} from {currentRole} to {facts.TargetRole} gains {gainedCount} and loses {lostCount} right(s). {findings[0].Statement}{moreText} {ConfirmationNotice}";
    }

    private static IReadOnlyCollection<string> RolesToHold(string targetRole) =>
        targetRole == Roles.User ? [] : [targetRole];

    private static List<string> RightsOf(IReadOnlyCollection<string> roles) =>
        Permissions.ExpandRoles(roles).Where(right => !roles.Contains(right)).ToList();

    private static bool IsSettingsRight(string right) =>
        right == Permissions.CanViewSettings || right == Permissions.CanEditSettings;

    private static string Join(IEnumerable<string> rights) => string.Join(RightSeparator, rights);
}
