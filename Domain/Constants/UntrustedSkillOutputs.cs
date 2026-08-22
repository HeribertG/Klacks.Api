// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Curated set of skills whose results carry content authored OUTSIDE this system: web pages, e-mail
/// bodies and headers, chat messages. Their results are framed as untrusted when fed back into the LLM
/// loop so the model treats them as data instead of as instructions (prompt-injection defense).
/// Curated by name — mirroring the <c>SkillRiskClassifier.SensitiveSkills</c> precedent — because the
/// classification must also cover plugin skills that are seeded from JSON and therefore have no C#
/// implementation class or compile-time descriptor to annotate. A name heuristic is deliberately not
/// used: a future skill named <c>read_*</c> or <c>list_*</c> that reads internal data only would be
/// mislabeled, and worse, a renamed untrusted skill would silently lose the flag.
/// <c>UntrustedSkillOutputsCatalogueTests</c> guards every entry against skill renames.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class UntrustedSkillOutputs
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        // Search results from the open internet: title, snippet and URL are attacker-controlled text.
        "web_search",
        // E-mail subject, sender and body come from whoever sent the mail.
        "read_email",
        "fetch_new_emails",
        "translate_email",
        "list_emails",
        "list_emails_by_client",
        "list_emails_by_group",
        // Derived from an e-mail body, so it inherits the sender's text (summary, quoted content).
        "get_email_analysis",
        // Messaging plugin: message bodies written by other users or relayed from external channels.
        "read_messages"
    };

    public static bool Contains(string? skillName) =>
        !string.IsNullOrEmpty(skillName) && Names.Contains(skillName);

    public static IReadOnlyCollection<string> All => Names;
}
