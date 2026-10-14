// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skills that neither the Slack owner bridge nor an MCP caller may offer or run. Both channels cap the rights at
/// Authorised (Supervisor), and B8 opened exactly these skills to that role (messaging and the e-mail inbox, gated
/// CanEditClients): without this list the cap alone would hand a physically exposed chat or an external agent the
/// messenger store and the whole inbox of the installation. get_replacement_status is listed for a different reason:
/// its escalation-chain part decides admin through the database role (the planning audience resolver), which a capped
/// channel's reduced rights do not change, so a capped admin would see every chain. Skills the Supervisor already reached before B8
/// (list_emails, read_email, get_email_analysis, list_emails_by_client) are deliberately not listed. The list is the
/// one building block of both channels: McpSkillExposurePolicy adds it to its exclusions (catalog, call handler,
/// delegated plan and schedule steps, read-only research) and SkillExecutorService, the toolset filter of the chat
/// turn and the plan and schedule authoring skills ask Blocks for the Slack channel. Matching is by name, ordinal and
/// case-insensitive, like the other curated skill lists, because it must also cover plugin skills.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Constants;

public static class CappedChannelSkillExclusions
{
    public const string NotAvailableMessage = "Skill '{0}' is not available in this channel.";

    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "send_message",
        "read_messages",
        "list_email_folders",
        "mark_email_read",
        "move_email_to_folder",
        "delete_email",
        "restore_email",
        "fetch_new_emails",
        "translate_email",
        "list_emails_by_group",
        "get_email_group_tree",
        "get_replacement_status"
    };

    public static IReadOnlyCollection<string> All => Names;

    public static bool Contains(string? skillName) =>
        !string.IsNullOrEmpty(skillName) && Names.Contains(skillName);

    public static bool Blocks(
        string? skillName,
        string? cappedChannel,
        PersonalAccessTokenAccessMode? externalAgentAccessMode) =>
        (!string.IsNullOrEmpty(cappedChannel) || externalAgentAccessMode.HasValue) && Contains(skillName);

    public static bool Blocks(string? skillName, SkillExecutionContext context) =>
        Blocks(skillName, context.CappedChannel, context.ExternalAgentAccessMode);

    public static string DescribeNotAvailable(string skillName) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, NotAvailableMessage, skillName);
}
