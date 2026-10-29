// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Classifies skills into risk classes for autonomy gating. Order: explicit sensitive list,
/// scenario-gated writers (mutations land in an AnalyseScenario that a human accepts), fill-empty-only
/// system writers (FillEmptyOnlySkills),
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
        // close_period seals every Work/Break in the period. reopen_period restores the Confirmed/Approved
        // lock levels the seal recorded, but not for entries sealed before that level was recorded (they
        // reopen to None). So a close stays
        // only partly reversible despite the inverse mapping.
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
        // Owner decision 2026-10-25: the two assignment skills are the targets of the absence-hours and target-hours
        // advisory skills, whose findings and knowledge texts promise one confirmation before a contract is assigned
        // (an assignment deactivates the previously active contract and changes the person's target from that day).
        // Sensitive is the only mechanism that enforces that promise at every autonomy level (precedent: create_group).
        "assign_contract_to_client",
        "assign_contract_by_name",
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
        // create_contract_from_template is the target evaluate_contract_templates funnels toward, the same
        // shape as create_group above: the advisory skill's description and the contract-setup knowledge
        // both promise that nothing is created before the administrator has chosen the template and the
        // adapted values and the system has asked for the final confirmation, and nothing short of
        // Sensitive enforces that promise - without it a
        // vaguely phrased request could chain evaluate_contract_templates straight into creation with no
        // human turn in between. The new contract is wage-base master data (hours and time-credit basis)
        // that persons are assigned to afterwards. Its delete_contract inverse stays registered, so the
        // undo offer for administrators is unchanged; Sensitive outranks a registered inverse.
        "create_contract_from_template",
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
        // seal_shift also carries a server preview (SealShiftConfirmationPreviewProvider): it seals only as the
        // confirmed replay of what that preview showed, and no agent plan can run it. seal_open_orders, the batch
        // form, is switched off in its seed (owner decision 2026-10-23, no mass sealing by Klacksy) and is
        // therefore not listed here; should it ever be switched on again it falls through to the fail-closed
        // Sensitive default and still seals only as the confirmed replay of its own server preview.
        "seal_shift",
        "set_sealed_order_until_date",
        // execute_order_review changes, seals and cuts the orders of the guided order review - the only way Klacksy seals
        // orders since 2026-10-23. It carries a server preview (OrderReviewConfirmationPreviewProvider) and runs only as
        // the confirmed replay of what that preview showed; it is deliberately NOT a preview/apply pair
        // (PreviewApplySkillCalls), the preview is the gate's own.
        "execute_order_review",
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
        // GuaranteedHours; the overtime, surcharge and compensatory-rest cards decide how extra time is
        // worked out; update_owner_locale_settings switches the GLOBAL holiday calendar and
        // import_calendar_rules bulk-writes the holiday definitions that calendar feeds on.
        "update_contract",
        "create_macro",
        "update_macro",
        "extend_macro",
        "assign_macro_to_shift",
        "assign_macro_to_absence_type",
        "revert_macro_assignment",
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
        // The planning deadline lead decides how many days before a period starts the next-period detector
        // reports it and, when the autonomy brakes allow, starts the automatic autofill: raising it moves
        // the autopilot's start earlier, onto less complete availability input. The guided recipe only
        // calculates and offers; storing is a separate request the user makes afterwards, and the
        // confirmation gate holds that call until the user has said yes.
        "set_planning_deadline_lead",
        // The period close lag is the stored proof that the user was asked when periods are closed: it
        // moves the close reminders and, at autonomy level FullyAutonomous, the automatic close of a
        // period, which is not cleanly reversible (see the close_period entry above).
        // Storing it is a separate request the user makes; the confirmation gate holds the call until
        // the user has said yes.
        "set_period_close_lag",
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
        // stash_pending_note can address a note at a named user or at every user of the installation, and
        // such a note is read back and relayed by Klacksy on the recipient's next turn - text placed in
        // front of somebody else by an assistant reads as the assistant's own word. The skill body now
        // refuses a foreign recipient to anyone but an administrator, which closes the Planer-floor hole;
        // this entry closes the two paths a body gate cannot reach. Sensitive is the only class
        // UnattendedSkillPolicy refuses on every background path regardless of level or opt-in, and the
        // only one McpSkillExposurePolicy hides - and /mcp accepts personal-access-token authentication at
        // the role-less floor. The proactive report path is unaffected: ProactiveActionReporter writes
        // through IPendingUserNoteRepository directly and never calls this skill. Price, stated plainly:
        // an ordinary self-note now costs a chat confirmation at every autonomy level, the same friction
        // that got apply_grouping removed from this list.
        "stash_pending_note",
        // send_message hands a message to Telegram, WhatsApp, Signal or SMS. It leaves the installation
        // and cannot be recalled - the outward-facing, irrevocable shape that put create_donation_checkout
        // above, and the reason is the same: an assistant that can reach a client's phone unattended must
        // not be fail-open.
        "send_message",
        // Owner decision 2026-10-12 ("only deletes and resets ask"): the default autonomy level stays Autonomous, but
        // every delete_/remove_/reset_ skill that destroys business, personal, payroll, compliance or credential data is
        // confirmed at EVERY level - the assistant must not remove such data on its own say-so or a prompt injection.
        // What stays Irreversible is assistant-internal bookkeeping only (delete_ai_memory, delete_agent_skill,
        // delete_transcription_dictionary_entry, see DestructiveSkillRiskDecisionGuardTests). Price, stated plainly:
        // UnattendedSkillPolicy refuses these on every background path, McpSkillExposurePolicy hides them from MCP and
        // ScheduleRecurringTaskSkill cannot schedule them.
        // Client master data and links: free text, contact data, hour basis and planning eligibility.
        "delete_address",
        "delete_communication",
        "delete_annotation",
        "delete_qualification",
        "remove_client_contract",
        "remove_client_qualification",
        "remove_client_from_group",
        // Shift and container structure: delete_shift refuses shifts with cuts or works, but the shift row itself is
        // gone; the resets rebuild or delete sub-shifts and day overrides.
        "delete_shift",
        "reset_shift_cuts",
        "reset_container_day",
        "remove_shift_from_group",
        "remove_container_template_task",
        "remove_shift_required_qualification",
        // Owner decision 2026-10-23 ("cutting always with confirmation"): a cut turns the plannable shift of an order
        // into parts, and every part carries its own copy of the default expenses, so each part is booked with them;
        // undoing it takes reset_shift_cuts, which is Sensitive itself. Held at every level. Price, stated plainly: the
        // hard-coded recipe "dienst-aus-bestellung-schneiden" runs over three turns - its request turn ends on the held seal,
        // the turn that confirms the seal forces the cut and ends on its hold (CutRecipeContinuationResolver), the user
        // confirms the cut in the turn after that -, UnattendedSkillPolicy refuses both cuts on every background path, McpSkillExposurePolicy
        // hides them from MCP, ScheduleRecurringTaskSkill cannot schedule them and a plan step pauses for approval.
        "cut_shift",
        "cut_shift_by_date",
        // Schedule day-to-day deletes that carry content or payroll weight (expenses, work changes, notes, planned
        // absences). delete_work is Reversible through its registered inverse restore_work (InverseSkillRegistry) and
        // delete_break stays Reversible (ReversibleExtras) without a registered inverse of its own: it is the inverse of
        // add_break, and the removed entry is planned again with that. bulk_delete_works below has no inverse at all
        // (a restore is one work at a time inside an undo window), so it is held at every level.
        "delete_workchange",
        "delete_expense",
        "delete_schedule_note",
        "delete_break_placeholder",
        // bulk_delete_works removes up to BulkWorkDeletionDefaults.MaxWorksPerCall works of one employee with one
        // sentence. Unlike delete_work it has no inverse: restore_work brings back one work at a time, for the deleting
        // user only inside the undo window (InverseSkillRegistry answers it with a __manual__ entry). It is held at
        // every level with the server-computed list as it is at the time of the confirmation, which the confirmed call
        // works out again (BulkWorkDeletionConfirmationPreviewProvider), so it has no apply=false preview of its own
        // and is not in PreviewApplySkillCalls.
        "bulk_delete_works",
        // Absence master data, compliance rules, periods and layouts: the rule deletes switch off a labour-law check,
        // the others lose hand-built configuration the delete does not echo back.
        "delete_absence",
        "delete_absence_type",
        "delete_counter_rule",
        "delete_period_cap_rule",
        "delete_restricted_time_window_rule",
        "delete_scheduling_rule",
        "delete_individual_period",
        "delete_report_template",
        "delete_export_format_override",
        // LLM provider and model rows hold the encrypted API key and can switch the assistant itself off.
        "delete_llm_provider",
        "delete_llm_model",
        // Secret writers: these skills take an API key or a mail password as a parameter. The secret has already
        // travelled through the chat by then, so the confirmation cannot undo that; it stops the unasked write that
        // would redirect a mail server, swap a provider key or silently disable a service.
        "update_deepl_settings",
        "update_web_search_settings",
        "update_openroute_settings",
        "update_speech_settings",
        "update_email_settings",
        "update_imap_settings",
        "create_llm_provider",
        "update_llm_provider",
        "create_llm_model",
        "update_llm_model"
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

    // Writers that only fill EMPTY system-derived values and never overwrite anything a person entered
    // (owner decision 2026-10-24): they run without confirmation at every autonomy level, like a read, but are
    // writes everywhere else (never in read-only toolsets, MCP read mode or the learning replay).
    // geocode_addresses gives addresses without coordinates the centroid of their postcode and city through
    // IAddressGeocodingService: the write is conditional on still-empty coordinates and unchanged postcode/city/
    // country, it is a system write (audited in address_geocoding_state with the user it ran for), employee
    // addresses only when the installation allows it, and Klacksy reports what it wrote ("without asking, but
    // not without telling"). Its worst case is an approximate map position that any address edit replaces.
    internal static readonly HashSet<string> FillEmptyOnlySkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "geocode_addresses"
    };

    internal static readonly HashSet<string> ReversibleExtras = new(StringComparer.OrdinalIgnoreCase)
    {
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
    // DraftPersistingReadOnlySkills is spread in: the company-rule and planning-profile intake steps
    // write the owner's persisted draft row (not an in-memory draft any more) and create_plan stores a draft
    // plan and returns its own confirmation request without running it. They stay un-gated here so every
    // dialog turn and the plan proposal do not cost a confirmation, but they are NOT write-free - consumers
    // that need "writes nothing" (read-only tokens, the research sub-loop) exclude that set.
    internal static readonly HashSet<string> ReadOnlyExtras = new(DraftPersistingReadOnlySkills.Names.Concat(
    [
        "find_customer_candidates",
        "find_split_shift_candidates",
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
        "list_messaging_providers",
        // diagnose_messaging_setup only reads provider rows, stored messages and the in-memory inbound
        // tracker, and calls vendor GET endpoints (getMe, auth.test, bot/info, ...); it never registers a
        // webhook and never tests Teams, whose check would post. A confirmation here would stall exactly
        // the untrained admin it exists for.
        "diagnose_messaging_setup"
    ]), StringComparer.OrdinalIgnoreCase);

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
        // qualification link). Single-row, id- or name-addressed creates and updates; a wrong value is corrected by
        // the paired update skill and nothing computed is rewritten. Their deletes and link removals are Sensitive.
        "create_employee",
        "update_client",
        "update_client_birthdate",
        "update_client_gender",
        "update_client_type",
        "create_address",
        "update_address",
        "add_client_email",
        "add_client_phone",
        "add_client_note",
        "update_communication",
        "update_annotation",
        "set_client_qualification",
        "create_qualification",
        "update_qualification",
        // Group tree, branches and membership links. Every entry writes one link row or one group field; deleting a
        // group, branch or membership and removing a link are Sensitive.
        "add_client_to_group",
        "add_client_to_group_by_name",
        "add_client_to_nearest_group",
        "add_shift_to_group",
        "update_group",
        "move_group",
        "set_group_location",
        "update_branch",
        "geocode_location_groups",
        // Bulk group writers that default to apply=false. Classify() only sees the name, never the
        // parameters, so Sensitive would gate their read-only preview call too and break the dry-run-then-apply
        // idiom (owner decision, see AutonomyGateServiceTests).
        "apply_grouping",
        "apply_grouping_plan",
        "fill_group_by_criteria",
        "group_ungrouped_by_city_name",
        "partition_clients_by_address",
        "partition_clients_by_qualification",
        "bulk_add_shifts_to_group",
        "bulk_add_absence_for_group",
        "add_selected_clients_to_group",
        // Same bulk-writer shape for the ERP-imported orders: assign_orders_to_groups derives the group
        // of every unlinked open order from its customer's address and defaults to apply=false, so
        // Sensitive would gate its preview too. Each run writes one group_item row per order; a wrong
        // placement is corrected by remove_shift_from_group and add_shift_to_group.
        "assign_orders_to_groups",
        // The shift counterpart moves sealed orders, plannable shifts and split shifts into the city group of
        // their customer's address and replaces their current group links. It also defaults to apply=false, so
        // Sensitive would gate the preview too; a wrong move is corrected by remove_shift_from_group and
        // add_shift_to_group.
        "assign_shifts_to_city_groups",
        // Shift and container structure: creates, updates and template additions. The cuts and the destructive
        // counterparts (cut_shift, cut_shift_by_date, delete_shift, reset_shift_cuts, reset_container_day,
        // remove_container_template_task) are Sensitive. create_shift creates only an open, still editable order and
        // never seals (owner decision 2026-10-24); the one-way sealing step is seal_shift, which is Sensitive and bound
        // to a confirmed preview.
        "create_shift",
        "update_shift",
        "bundle_nearby_timerange_shifts_into_container",
        "add_container_template_task",
        // optimize_container_route rewrites the visit and travel times of one weekday template (re-enterable, the next
        // optimization or the container page rewrites them again); it never adds or removes a visit.
        "optimize_container_route",
        "set_shift_required_qualification",
        "set_shift_preferences",
        // Schedule day-to-day writes: works, work changes, expenses, key commands, notes and the
        // scenario accept/reject pair. Single-row and re-enterable; period hours are recalculated automatically.
        // The deletes that carry no registered inverse (work change, expense, note) are Sensitive.
        "update_work",
        "update_workchange",
        "add_workchange",
        "update_expense",
        "add_schedule_command",
        "add_schedule_commands_range",
        "add_schedule_note",
        "update_schedule_note",
        "accept_scenario",
        "reject_scenario",
        // Absence master data, absence bookings and availability: creates and updates only; the deletes are
        // Sensitive.
        "create_absence",
        "update_absence",
        "create_absence_type",
        "update_absence_type",
        "update_break",
        "set_client_availability",
        // Rule and period master data: creates and updates. The evaluators only raise warnings or refuse an
        // assignment and never rewrite recorded hours or wages; switching one off by delete is Sensitive.
        // create_calendar_selection stays here by owner decision: a brand-new, still-unassigned calendar
        // selection cannot yet affect any payroll run.
        "create_counter_rule",
        "update_counter_rule",
        "create_period_cap_rule",
        "update_period_cap_rule",
        "create_restricted_time_window_rule",
        "update_restricted_time_window_rule",
        "create_scheduling_rule",
        "update_scheduling_rule",
        "create_individual_period",
        "update_individual_period",
        "create_calendar_selection",
        // Report layouts and export-format patches. No business data and no computed value is touched.
        "create_report_template",
        "update_report_template",
        "set_export_format_override",
        // Inbox handling that moves or flags one message. Nothing leaves the installation and no message
        // is destroyed - permanent deletion deliberately has no skill.
        "create_email_folder",
        "mark_email_read",
        "move_email_to_folder",
        "restore_email",
        "fetch_new_emails",
        // Settings cards without payroll, identity, mandate or secret reach. A wrong value is corrected by
        // calling the same skill again; the payroll-relevant cards and every card that takes an API key or a
        // password live in SensitiveSkills instead.
        "update_general_settings",
        "update_scheduling_defaults",
        "update_work_settings",
        "update_wizard_settings",
        "update_grid_color_settings",
        "update_owner_address",
        "update_active_industries_settings",
        "update_export_formats_settings",
        "update_spam_filter_settings",
        "set_my_theme",
        "set_my_display_language",
        // The assistant's own bookkeeping: memories, notes, user-created UiAction skills, skill
        // relations, navigation synonyms, the transcription dictionary, scheduled tasks and the feature-plugin
        // on/off toggle. confirm_pending_action is listed for completeness only - AutonomyGateService
        // short-circuits it by name before any classification is read.
        "add_personal_memory",
        ExplainDepthDefaults.SetSkillName,
        "update_ai_memory",
        "delete_ai_memory",
        "manage_pending_notes",
        "create_agent_skill",
        "update_agent_skill",
        "delete_agent_skill",
        "accept_skill_relation",
        "dismiss_skill_relation",
        "close_clarification",
        "update_navigation_synonyms",
        "add_transcription_dictionary_entry",
        "update_transcription_dictionary_entry",
        "delete_transcription_dictionary_entry",
        "confirm_pending_action",
        "schedule_recurring_task",
        "cancel_recurring_task",
        "enable_feature_plugin",
        "disable_feature_plugin",
        // LLM default-model selection and model optimisation. Creating, updating and deleting provider and model rows
        // (they hold the API key) is Sensitive.
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

        if (FillEmptyOnlySkills.Contains(descriptor.Name))
        {
            return SkillRiskClass.FillEmptyOnly;
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

        return InverseSkillRegistry.TryGetRollbackEntry(skillName, out var inverse)
            && !string.Equals(inverse.SkillName, InverseSkillRegistry.ManualMarker, StringComparison.Ordinal);
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
