// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies skills into risk classes for autonomy gating. Order: explicit sensitive list,
/// scenario-gated writers (mutations land in an AnalyseScenario that a human accepts),
/// reversible skills (InverseSkillRegistry mapping or explicit extras), read-only detection
/// (explicit allow-list, then read-only categories; a read-only name prefix is ignored for
/// write categories so a write skill cannot bypass the gate via its name), then the explicit
/// irreversible list.
/// Anything left over is SENSITIVE, not irreversible. That last step is the fail-closed one: at the
/// factory-default autonomy level (Autonomous) an Irreversible skill runs with NO confirmation, so a
/// write skill that nobody classified used to reach live data unasked simply by existing. A skill only
/// earns that unconfirmed execution by being written into IrreversibleSkills on purpose; an unlisted
/// one is held at every level and refused on every unattended path until somebody decides.
/// WriteSkillRiskDecisionCoverageTests keeps that from being a silent surprise: it fails by name for any
/// write-category seed skill missing from all five collections, so a new skill is caught at test time
/// rather than turning Sensitive in production.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.Meta;

public class SkillRiskClassifier : ISkillRiskClassifier
{
    private const string ManualInverseMarker = "__manual__";

    internal static readonly HashSet<string> SensitiveSkills = new(StringComparer.OrdinalIgnoreCase)
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
        "delete_container_template",
        // create_donation_checkout starts a real payment flow: it opens a Stripe Checkout session for a
        // concrete amount and hands back its hosted payment URL. Owner decision — Sensitive, and the
        // reason is unattended execution, not irreversibility. Since stage 5a the risk class is what
        // decides whether a skill may run with nobody watching (a scheduled task or Klacksy's heartbeat):
        // Reversible passes from autonomy level Autonomous upwards and the dev default already sits at
        // FullyAutonomous, while the classifier's fall-through default, Irreversible, still slips through
        // a scheduled task that carries the per-task opt-in. Only Sensitive is unconditionally closed —
        // UnattendedSkillPolicy refuses it on every background path regardless of level or opt-in, and
        // AutonomyGateService demands an explicit user confirmation in chat at every level. An assistant
        // that can initiate a payment flow on its own must not be fail-open.
        "create_donation_checkout",
        // Sealing is a one-way lifecycle transition with no counterpart skill: seal_shift turns an order
        // into a permanently immutable SealedOrder, and set_sealed_order_until_date is the single change
        // that row ever accepts again ("only allowed once", and only while no work exists after the date).
        // Both guard their data-losing edge in the handler, but neither can be undone at all afterwards.
        "seal_shift",
        "set_sealed_order_until_date",
        // Membership dates are the plannability boundary - the exact reason delete_membership is listed
        // above. end_client_membership writes validUntil on the active membership without needing its
        // UUID, and update_membership can move either end of the affiliation period, so both move that
        // boundary just as a delete would.
        "end_client_membership",
        "update_membership",
        // Payroll-relevant master data and computation settings, same blast radius that put
        // delete_contract, delete_macro, delete_monthly_target_hours and update_calendar_selection above:
        // a wrong value here silently moves figures that were already computed against it. Macros are the
        // calculation scripts; contract templates are the wage base; monthly target hours short-circuit
        // GuaranteedHours; the overtime, surcharge and compensatory-rest cards decide how extra pay is
        // worked out; update_owner_locale_settings switches the GLOBAL holiday calendar and
        // import_calendar_rules bulk-writes the holiday definitions that calendar feeds on.
        "update_contract",
        "create_macro",
        "update_macro",
        "create_monthly_target_hours",
        "update_monthly_target_hours",
        "update_overtime_settings",
        "update_surcharge_mode_settings",
        "update_compensatory_rest_settings",
        "update_owner_locale_settings",
        "import_calendar_rules",
        // create_spam_rule and update_spam_rule call the very same TriggerReclassification() that put
        // delete_spam_rule above (CreateSpamRuleCommandHandler.cs:49, UpdateSpamRuleCommandHandler.cs:48):
        // an unawaited background sweep that moves mail on the IMAP server, assigns senders to clients and
        // notifies users, none of it reported back.
        "create_spam_rule",
        "update_spam_rule",
        // Klacksy never widens its own mandate - the set_proactive_governance rule above, applied to the
        // three other switches that decide what it may do. update_ai_guidelines REPLACES its instructions
        // and update_ai_soul rewrites a section of its personality (both feed the system prompt, including
        // every "ask first" clause in it); update_compliance_enforcement_settings decides whether breaking
        // a working-time protection only warns instead of refusing the assignment, i.e. it is the brake on
        // the labour-law rules the planner obeys.
        "update_ai_guidelines",
        "update_ai_soul",
        "update_compliance_enforcement_settings",
        // Installing a feature plugin ADDS SKILLS to the assistant (the messaging plugin is what brings
        // send_message), which is mandate widening in its most literal form; uninstalling removes them
        // again together with the plugin's data. The Whisper pair is the same decision one layer down: it
        // puts speech-transcription software on the host, and removing it sends spoken input back out to
        // an external transcription service.
        "install_feature_plugin",
        "uninstall_feature_plugin",
        "install_whisper_plugin",
        "uninstall_whisper_plugin",
        // update_user changes SOMEBODY ELSE's login name and email address - the same identity surface as
        // create_user and delete_system_user above, and the address every password reset is sent to.
        // update_my_account writes the SAME fields on the caller's own login: being the owner of the
        // account does not make a redirected password-reset address or a changed sign-in name any less of
        // a lockout, so the self-service variant is not the softer case.
        "update_user",
        "update_my_account",
        // The retention setting is the only settings value whose change destroys rows for good:
        // DataRetentionBackgroundService issues a raw DELETE FROM over every soft-deleted row older than
        // the configured window (DataRetentionBackgroundService.cs:102). Lowering it purges data that soft
        // delete had kept recoverable.
        "update_data_retention_settings",
        // clear_client_availability wipes EVERY availability row of a client across a whole date range in
        // one call. Its clear_ name keeps it out of the delete_/remove_ guard, but the blast radius is the
        // bulk, no-restore one that put delete_container_template above: set_client_availability only
        // rebuilds it day by day and hour by hour, from information the employee supplied.
        "clear_client_availability",
        // send_message hands a message to Telegram, WhatsApp, Signal or SMS. It leaves the installation
        // and cannot be recalled - the outward-facing, irrevocable shape that put create_donation_checkout
        // above, and the reason is the same: an assistant that can reach a client's phone unattended must
        // not be fail-open.
        "send_message"
        // apply_grouping was listed here and is deliberately NOT anymore (owner decision): unlike every
        // other entry it is the second half of a propose/apply pair, so the user has already seen the
        // full preview (which clients, which target groups, how many memberships end) and approved it
        // before the call is made. The extra gate therefore asked a second time for something already
        // confirmed, and — because a tool result carrying the token does not survive into the next
        // turn's history — it repeatedly failed to be redeemed, forcing the user through six rounds of
        // "yes" for a single action. Re-running the apply is a no-op once everyone sits in their target
        // group, which keeps the blast radius of a mistaken repeat at zero.
    };

    internal static readonly HashSet<string> ScenarioGatedSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "propose_plan",
        "start_autowizard",
        "start_wizard1",
        "start_wizard2",
        "start_wizard3",
        "cover_absence"
    };

    internal static readonly HashSet<string> ReversibleExtras = new(StringComparer.OrdinalIgnoreCase)
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
    internal static readonly HashSet<string> ReadOnlyExtras = new(StringComparer.OrdinalIgnoreCase)
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
        "start_guided_tour",
        // rollback_my_last_change only LOOKS UP the inverse of the last successful execution and returns
        // it as a proposal; it never calls anything (see the skill itself). Same reason as create_plan
        // above - the inverse call it suggests runs through this gate on its own.
        "rollback_my_last_change",
        // The messaging plugin seeds category "Communication", which SkillCategory does not know, so
        // SkillRegistryInitializer.ParseCategory falls back to Action - a WRITE category. That makes the
        // read-only name prefix worthless for these two and used to classify a plain read as Irreversible.
        // Both were verified to only read (ReadMessagesSkill queries messages, ListMessagingProvidersSkill
        // queries providers); the plugin's writer, send_message, is Sensitive instead.
        "read_messages",
        "list_messaging_providers"
    };

    // Write skills deliberately allowed to run UNCONFIRMED at the factory-default autonomy level
    // (Autonomous), and on a scheduled task that carries the per-task opt-in. This is the list that keeps
    // the fall-through in Classify() fail-closed: leaving a write skill out of every collection no longer
    // grants it unconfirmed execution, it makes it Sensitive until somebody decides. Entries belong here
    // when the write is an ordinary, single-scope create or update of master or planning data whose worst
    // case is a wrong value that the paired skill corrects - not something permanent, destructive,
    // payroll-, identity- or mandate-relevant, and not something that acts outside the installation.
    // The destructive delete_/remove_ entries additionally carry a written, per-skill justification in
    // DestructiveSkillRiskDecisionGuardTests.AcceptedIrreversibleDeletes, which is where that detail lives
    // instead of here.
    internal static readonly HashSet<string> IrreversibleSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        // Client master data and its sub-resources (address, communication, annotation, contract link,
        // qualification link). Single-row, id- or name-addressed writes; a wrong value is corrected by the
        // paired update skill and nothing computed is rewritten.
        "create_employee",
        "update_client",
        "update_client_birthdate",
        "update_client_gender",
        "update_client_type",
        "create_address",
        "update_address",
        "delete_address",
        "add_client_email",
        "add_client_phone",
        "add_client_note",
        "update_communication",
        "delete_communication",
        "update_annotation",
        "delete_annotation",
        "assign_contract_to_client",
        "assign_contract_by_name",
        "remove_client_contract",
        "set_client_qualification",
        "remove_client_qualification",
        "create_qualification",
        "update_qualification",
        "delete_qualification",
        // Group tree, branches and membership links. Every entry writes one link row or one group field; the
        // cascading deletes that made delete_group/delete_branch/delete_membership Sensitive are not here.
        "add_client_to_group",
        "add_client_to_group_by_name",
        "add_client_to_nearest_group",
        "remove_client_from_group",
        "add_shift_to_group",
        "remove_shift_from_group",
        "update_group",
        "move_group",
        "set_group_location",
        "update_branch",
        "geocode_location_groups",
        // Bulk group writers that default to apply=false. Classify() only sees the name, never the
        // parameters, so Sensitive would gate their read-only preview call too and break the dry-run-then-apply
        // idiom (owner decision, see AutonomyGateServiceTests).
        "apply_grouping",
        "fill_group_by_criteria",
        "group_ungrouped_by_city_name",
        "bulk_add_shifts_to_group",
        "bulk_add_absence_for_group",
        "add_selected_clients_to_group",
        // Shift and container structure. delete_shift, reset_container_day and
        // remove_container_template_task all refuse the data-losing case in the handler itself (assigned works,
        // existing cuts), so the destructive edge is blocked below this classification.
        "create_shift",
        "update_shift",
        "delete_shift",
        "cut_shift",
        "cut_shift_by_date",
        "reset_shift_cuts",
        "bundle_nearby_timerange_shifts_into_container",
        "add_container_template_task",
        "remove_container_template_task",
        "reset_container_day",
        "set_shift_required_qualification",
        "remove_shift_required_qualification",
        "set_shift_preferences",
        // Schedule day-to-day writes: works, work changes, expenses, key commands, notes and the
        // scenario accept/reject pair. Single-row and re-enterable; period hours are recalculated automatically.
        "update_work",
        "update_workchange",
        "add_workchange",
        "delete_workchange",
        "update_expense",
        "delete_expense",
        "add_schedule_command",
        "add_schedule_commands_range",
        "add_schedule_note",
        "update_schedule_note",
        "delete_schedule_note",
        "accept_scenario",
        "reject_scenario",
        // Absence master data, absence bookings and availability. The delete skills refuse types still
        // used by active bookings, so no booked absence can be orphaned.
        "create_absence",
        "update_absence",
        "delete_absence",
        "create_absence_type",
        "update_absence_type",
        "delete_absence_type",
        "update_break",
        "delete_break_placeholder",
        "set_client_availability",
        // Rule and period master data. The evaluators only raise warnings or refuse an assignment; they
        // never rewrite recorded hours or wages, and the paired create skill restores every field identically.
        // create_calendar_selection stays here by owner decision: a brand-new, still-unassigned calendar
        // selection cannot yet affect any payroll run.
        "create_counter_rule",
        "update_counter_rule",
        "delete_counter_rule",
        "create_period_cap_rule",
        "update_period_cap_rule",
        "delete_period_cap_rule",
        "create_restricted_time_window_rule",
        "update_restricted_time_window_rule",
        "delete_restricted_time_window_rule",
        "create_scheduling_rule",
        "update_scheduling_rule",
        "delete_scheduling_rule",
        "create_individual_period",
        "update_individual_period",
        "delete_individual_period",
        "create_calendar_selection",
        // Report layouts and export-format patches. No business data and no computed value is touched.
        "create_report_template",
        "update_report_template",
        "delete_report_template",
        "set_export_format_override",
        "delete_export_format_override",
        // Inbox handling that moves or flags one message. Nothing leaves the installation and no message
        // is destroyed - permanent deletion deliberately has no skill.
        "create_email_folder",
        "mark_email_read",
        "move_email_to_folder",
        "restore_email",
        "fetch_new_emails",
        // Settings cards without payroll, identity or mandate reach. A wrong value is corrected by
        // calling the same skill again; the payroll-relevant cards live in SensitiveSkills instead.
        "update_general_settings",
        "update_scheduling_defaults",
        "update_work_settings",
        "update_wizard_settings",
        "update_grid_color_settings",
        "update_owner_address",
        "update_active_industries_settings",
        "update_export_formats_settings",
        "update_email_settings",
        "update_imap_settings",
        "update_spam_filter_settings",
        "update_deepl_settings",
        "update_web_search_settings",
        "update_openroute_settings",
        "update_speech_settings",
        "set_my_theme",
        "set_my_display_language",
        // The assistant's own bookkeeping: memories, notes, user-created UiAction skills, skill
        // relations, navigation synonyms, the transcription dictionary, scheduled tasks and the feature-plugin
        // on/off toggle. confirm_pending_action is listed for completeness only - AutonomyGateService
        // short-circuits it by name before any classification is read.
        "add_personal_memory",
        "update_ai_memory",
        "delete_ai_memory",
        "stash_pending_note",
        "manage_pending_notes",
        "create_agent_skill",
        "update_agent_skill",
        "delete_agent_skill",
        "review_skill_suggestions",
        "accept_skill_relation",
        "dismiss_skill_relation",
        "update_navigation_synonyms",
        "add_transcription_dictionary_entry",
        "update_transcription_dictionary_entry",
        "delete_transcription_dictionary_entry",
        "confirm_pending_action",
        "schedule_recurring_task",
        "cancel_recurring_task",
        "enable_feature_plugin",
        "disable_feature_plugin",
        // LLM provider and model configuration. Technical admin configuration with no business data and
        // no cascade; a wrong row is corrected or removed by its paired skill.
        "create_llm_provider",
        "update_llm_provider",
        "delete_llm_provider",
        "create_llm_model",
        "update_llm_model",
        "delete_llm_model",
        "set_default_llm_model",
        "optimize_llm_models_for_klacksy",
        // Operational triggers and probes: the ERP import schedule and its manual run, the drop-point
        // folder check (which creates the folder when it is missing), the autofill test-environment builder and
        // the two UiActions that only open a page or a printable export.
        "set_erp_import_schedule",
        "trigger_erp_import_run",
        "update_erp_drop_point_settings",
        "check_erp_drop_point_folder_health",
        "create_test_environment",
        "open_schedule",
        "open_absence_calendar_pdf_export"
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

        if (IrreversibleSkills.Contains(descriptor.Name))
        {
            return SkillRiskClass.Irreversible;
        }

        return SkillRiskClass.Sensitive;
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
