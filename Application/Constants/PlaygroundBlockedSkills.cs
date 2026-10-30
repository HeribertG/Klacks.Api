// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Skills that must not run on the public Playground. Every visitor there is the shared demo admin, so a skill that
/// stores visitor text in a shared table (memories, notes, agent skills, synonyms, scheduled reminders) or reads it back
/// would hand one visitor's input to the next, and a skill that changes the assistant setup, the models and keys, the
/// users or the installed plugins would let any visitor spend the operator's money or lock everybody out. The list
/// mirrors the controllers that are already blocked on the Playground, which skills would otherwise bypass through the
/// mediator. Readers whose writers are all listed stay open because they only return the seeded demo content. The
/// business demo skills (employees, shifts, groups, absences ...) are deliberately not listed: the nightly reset wipes them.
/// </summary>
public static class PlaygroundBlockedSkills
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "create_plan",
        "send_broadcast",
        "translate_email",
        "test_imap_connection",
        "test_smtp_connection",
        "update_openroute_settings",
        "update_speech_settings",
        "update_web_search_settings",

        "add_ai_memory",
        "add_personal_memory",
        "update_ai_memory",
        "delete_ai_memory",
        "get_ai_memories",
        "update_ai_soul",
        "update_ai_guidelines",
        "create_agent_skill",
        "update_agent_skill",
        "delete_agent_skill",
        "stash_pending_note",
        "manage_pending_notes",
        "recall_delivered_notes",
        "schedule_recurring_task",
        "update_navigation_synonyms",
        "verify_my_last_action",
        "rollback_my_last_change",

        "create_llm_provider",
        "update_llm_provider",
        "delete_llm_provider",
        "create_llm_model",
        "update_llm_model",
        "delete_llm_model",
        "set_default_llm_model",
        "optimize_llm_models_for_klacksy",
        "add_transcription_dictionary_entry",
        "update_transcription_dictionary_entry",
        "delete_transcription_dictionary_entry",
        "set_autonomy_level",
        "set_proactive_governance",
        "accept_skill_relation",
        "dismiss_skill_relation",

        "create_user",
        "update_user",
        "delete_system_user",
        "assign_user_permissions",
        "update_my_account",
        "create_personal_access_token",
        "revoke_personal_access_token",
        "create_identity_provider",
        "update_identity_provider",
        "delete_identity_provider",

        "install_feature_plugin",
        "uninstall_feature_plugin",
        "enable_feature_plugin",
        "disable_feature_plugin",
        "install_language_pack",
        "uninstall_language_pack",
        "install_whisper_plugin",
        "uninstall_whisper_plugin",
        "start_software_update",
    };

    public static bool IsBlocked(string? skillName) => !string.IsNullOrEmpty(skillName) && Names.Contains(skillName);
}
