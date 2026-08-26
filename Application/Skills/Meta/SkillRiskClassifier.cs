// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies skills into risk classes for autonomy gating. Order: explicit sensitive list,
/// scenario-gated writers (mutations land in an AnalyseScenario that a human accepts),
/// reversible skills (InverseSkillRegistry mapping or explicit extras), read-only detection
/// (explicit allow-list, then read-only categories; a read-only name prefix is ignored for
/// write categories so a write skill cannot bypass the gate via its name), everything else
/// defaults to irreversible.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.Meta;

public class SkillRiskClassifier : ISkillRiskClassifier
{
    private const string ManualInverseMarker = "__manual__";

    private static readonly HashSet<string> SensitiveSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "delete_system_user",
        "assign_user_permissions",
        "set_user_group_scope",
        "set_autonomy_level",
        // Klacksy never widens its own mandate. set_proactive_governance decides how far the
        // heartbeat may act per trigger kind and holds the global kill switch, so leaving it
        // unlisted would let an autonomous run grant itself Execute and switch its own brake off.
        // Sensitive is what makes that impossible: UnattendedSkillPolicy denies every sensitive
        // skill on the background paths, and AutonomyGateService asks a human at every level.
        // Klacksy may still PROPOSE a change through the approval queue.
        "set_proactive_governance",
        "create_identity_provider",
        "update_identity_provider",
        "delete_identity_provider",
        // Destructive, cascading or hard-to-undo structural deletes (deleting a whole org unit
        // cascades to its shifts, deleting a client removes a person and their data, deleting a
        // membership shifts the plannability boundary).
        // NOTE: listing a skill here forces a confirmation at EVERY autonomy level, including
        // FullyAutonomous - see AutonomyGateService.IsAllowed.
        "delete_group",
        "delete_branch",
        "delete_client",
        "delete_membership",
        // PAT mutations must stay exclusive to the JWT-authenticated REST endpoint
        // (PersonalAccessTokensController) — otherwise a PAT- or OAuth-authenticated MCP session
        // could mint itself a fresh token or revoke every remaining token of its owner, defeating
        // revocation and contradicting the "a PAT cannot manage PATs" rule the REST controller
        // already enforces. list_personal_access_tokens is deliberately NOT listed here (owner
        // decision): it reads the caller's own token METADATA only — the plaintext is never
        // persisted (PersonalAccessToken stores TokenHash), so there is nothing to hand out — and
        // ChatController is pinned to JWT bearer, so the stolen-token enumeration risk exists on
        // the /mcp path alone. That path is closed by McpSkillExposurePolicy's own exclusion list
        // instead, which keeps the skill invisible to MCP without charging a pure read a chat
        // confirmation.
        // create_personal_access_token no longer mints anything (it only points at the settings card,
        // because a plaintext token placed in a skill result would travel to the language-model
        // provider). It stays listed all the same: the entry is what makes "a chat or MCP session
        // cannot mint a credential" a structural guarantee instead of a property of the current
        // skill body, which is exactly the thing that regressed.
        "create_personal_access_token",
        "revoke_personal_access_token",
        // The ERP import token pair is the structural twin of the PAT pair — a separate token
        // universe (prefix klacks_erp_, scheme KlacksErpImport) whose secret authenticates the
        // order-upload endpoint — but it was classified Crud and therefore only Irreversible, which
        // passes at the Autonomous default level AND is exposed over /mcp. Since /mcp accepts
        // personal-access-token authentication, a stolen PAT could mint itself an upload credential
        // or revoke every key an external ERP vendor still holds, silently cutting off the import
        // pipeline until a new key is issued and redistributed. create_erp_import_token no longer
        // mints (same reason as its PAT twin) and is listed for the same durability reason.
        "create_erp_import_token",
        "revoke_erp_import_token",
        // close_period seals every Work/Break in the period; reopen_period does NOT restore
        // Confirmed/Approved lock levels, so a close is effectively lossy despite the inverse mapping.
        "close_period",
        // create_user mints a system login (attack surface + password-reset mail). Confirmed at every
        // level, like every Sensitive entry.
        "create_user",
        // Company-rule apply/revert persist settings, counter rules or macros and are only partially
        // reversible. Confirmed at every level, like every Sensitive entry.
        "apply_company_rule",
        "revert_company_rule",
        // apply_planning_profile creates real SchedulingRule rows and switches ACTIVE_INDUSTRIES to the
        // custom marker; it is only partially reversible, so a human must confirm it even at the
        // Autonomous default level.
        "apply_planning_profile",
        // update/delete_calendar_selection change or remove a holiday calendar that feeds directly into
        // payroll/surcharge calculation (MacroDataProvider); a wrong merge or an accidental delete would
        // silently change wages already computed against it, so a human must confirm even at the
        // Autonomous default level. create_calendar_selection is deliberately NOT listed here (owner
        // decision): a brand-new, still-unassigned calendar selection cannot yet affect any payroll run.
        "update_calendar_selection",
        "delete_calendar_selection",
        // Macros are the calculation scripts feeding surcharge and payroll computation — the same
        // blast radius that put the calendar-selection mutations here. Confirmed at every level, like
        // every Sensitive entry.
        "delete_macro",
        // Contract templates are wage-base master data (hour and surcharge basis); the skill itself
        // recommends validUntil over deletion, so an actual delete is rare enough that the
        // confirmation friction is low while a wrong delete would silently affect future computation.
        "delete_contract",
        // delete_email_folder is the only skill in the catalogue whose loss reaches past the database:
        // the handler calls DeleteFolderOnImapAsync, which removes the folder and every message still
        // living on the mail server for good. Locally synced copies survive under trash, so the damage
        // is invisible from inside Klacks — which is exactly why a human has to confirm it.
        "delete_email_folder",
        // Deleting a monthly target row removes the value that ResolveGuaranteedHours short-circuits
        // into GuaranteedHours for every contract on PaymentInterval.MonthlyTargetHours; without it
        // the chain falls back to rule/contract/defaults, which may be a completely different figure.
        // GuaranteedHours is resolved fresh on every call and feeds period hours, overtime and
        // surcharge computation alike, so a wrong delete silently moves numbers that were already
        // computed against it — the same blast radius that put delete_macro here.
        "delete_monthly_target_hours",
        // delete_spam_rule does more than drop a row: the handler always calls
        // TriggerReclassification, an unawaited background sweep over inbox, client-assigned and junk
        // folders that moves mail on the IMAP server, assigns senders to clients and notifies users —
        // none of it reported back. Re-creating the rule re-runs the sweep and converges, but by then
        // spam has already resurfaced in the inbox and the notifications have gone out.
        "delete_spam_rule",
        // create_group is the target two advisory skills (evaluate_location_group_candidates,
        // evaluate_grouping_by_qualification) funnel toward: their descriptions promise that creating
        // a group stays a separate, manual step, but nothing short of Sensitive enforces that promise —
        // without it, a vaguely phrased request could let the model chain evaluate_* straight into
        // create_group with no human turn in between. Not classified for irreversibility (a fresh,
        // still-empty group is in practice as undoable as create_calendar_selection, deliberately NOT
        // listed here for the same reason); the risk is specifically the automated funnel.
        "create_group",
        // delete_container_template is the registered inverse of create_container_template, but the REST
        // endpoint it drives is container-scoped: it deletes EVERY weekday template of the container in
        // one call, along with every task configured in them, and the handler does not cascade the items,
        // so a re-create leaves the old ones orphaned under a soft-deleted parent. That blast radius is
        // unaddressed and bulk, unlike every entry in DestructiveSkillRiskDecisionGuardTests'
        // AcceptedIrreversibleDeletes, which are all single-row id-addressed deletes — no honest
        // justification for running it unconfirmed at the default Autonomous level exists. Listing it
        // here does NOT weaken the reversibility it lends create_container_template: IsReversible only
        // asks whether an inverse is registered, never what class that inverse itself carries.
        "delete_container_template"
        // apply_grouping was listed here and is deliberately NOT anymore (owner decision): unlike every
        // other entry it is the second half of a propose/apply pair, so the user has already seen the
        // full preview (which clients, which target groups, how many memberships end) and approved it
        // before the call is made. The extra gate therefore asked a second time for something already
        // confirmed, and — because a tool result carrying the token does not survive into the next
        // turn's history — it repeatedly failed to be redeemed, forcing the user through six rounds of
        // "yes" for a single action. Re-running the apply is a no-op once everyone sits in their target
        // group, which keeps the blast radius of a mistaken repeat at zero.
    };

    private static readonly HashSet<string> ScenarioGatedSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "propose_plan",
        "start_autowizard",
        "start_wizard1",
        "start_wizard2",
        "start_wizard3",
        "cover_absence"
    };

    private static readonly HashSet<string> ReversibleExtras = new(StringComparer.OrdinalIgnoreCase)
    {
        "delete_work",
        "delete_break",
        "cancel_wizard_job",
        // delete_email only moves the mail into the trash folder (verified by re-reading it) and
        // restore_email brings it back; permanent deletion deliberately has no skill, so the
        // delete is factually reversible despite its destructive name.
        "delete_email"
    };

    // Skills whose names carry a read-only prefix but whose category is a write category (Crud).
    // They genuinely only read (list candidates), so they are allow-listed explicitly instead of
    // being trusted via the name prefix — which a future write skill could abuse.
    private static readonly HashSet<string> ReadOnlyExtras = new(StringComparer.OrdinalIgnoreCase)
    {
        "find_customer_candidates",
        "find_split_shift_candidates",
        // Company-rule intake steps that mutate only the ephemeral in-memory draft; without this
        // allow-listing their Crud-ish names would fall through to Irreversible and get gated.
        "start_company_rule",
        "set_company_rule_parameters",
        "cancel_company_rule",
        // Planning-profile intake steps that mutate only the ephemeral draft; without this allow-listing
        // their Crud-ish names would fall through to Irreversible and get gated every dialog turn.
        "start_planning_profile_setup",
        "set_planning_profile_parameters",
        "cancel_planning_profile_setup",
        // create_plan only DRAFTS a plan and returns its own confirmation request; it never runs the
        // plan itself. Execution starts only through confirm_pending_action, so the proposal call must
        // stay un-gated at every autonomy level to avoid a double confirmation.
        "create_plan",
        // start_guided_tour is a UiAction that only launches the onboarding tour overlay in the
        // browser — it mutates nothing. Its Action category would otherwise fall through to
        // Irreversible and gate a harmless tour start. (search_in_list and select_group, the other
        // non-mutating UiActions, already classify ReadOnly via their Query category.)
        "start_guided_tour"
    };

    private static readonly HashSet<SkillCategory> ReadOnlyCategories =
    [
        SkillCategory.Query,
        SkillCategory.Read,
        SkillCategory.Validation,
        SkillCategory.UI
    ];

    // Categories that always mutate state: a read-only name prefix must NOT classify them as
    // read-only (closes the trap where e.g. a Crud skill named "evaluate_*" would bypass the gate).
    private static readonly HashSet<SkillCategory> WriteCategories =
    [
        SkillCategory.Crud,
        SkillCategory.Action
    ];

    public SkillRiskClass Classify(SkillDescriptor descriptor)
    {
        if (SensitiveSkills.Contains(descriptor.Name))
        {
            return SkillRiskClass.Sensitive;
        }

        if (ScenarioGatedSkills.Contains(descriptor.Name))
        {
            return SkillRiskClass.ScenarioGated;
        }

        if (IsReversible(descriptor.Name))
        {
            return SkillRiskClass.Reversible;
        }

        if (IsReadOnly(descriptor))
        {
            return SkillRiskClass.ReadOnly;
        }

        return SkillRiskClass.Irreversible;
    }

    private static bool IsReversible(string skillName)
    {
        if (ReversibleExtras.Contains(skillName))
        {
            return true;
        }

        return InverseSkillRegistry.TryGet(skillName, out var inverse)
            && !string.Equals(inverse.SkillName, ManualInverseMarker, StringComparison.Ordinal);
    }

    private static bool IsReadOnly(SkillDescriptor descriptor)
    {
        if (ReadOnlyExtras.Contains(descriptor.Name))
        {
            return true;
        }

        if (ReadOnlyCategories.Contains(descriptor.Category))
        {
            return true;
        }

        if (WriteCategories.Contains(descriptor.Category))
        {
            return false;
        }

        return ReadOnlySkillPrefixes.HasReadOnlyPrefix(descriptor.Name);
    }
}
