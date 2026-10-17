// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Prompt texts for explanation depth: the stable offer-first and consequence-grounding rules every conversational prompt carries, and the
/// volatile per-user style block (one style sentence per experience level, whether the user chose it, and how the user
/// can change it for good).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ExplainDepthPromptTexts
{
    public const string OfferFirstGuide = """

EXPLANATIONS (mandatory):
- A direct question gets a direct answer at the depth it asks for — no preliminary offer.
- Never volunteer an explanation the user did not ask for. When one would clearly help, add at most ONE short,
  friendly offer sentence (e.g. "Shall I briefly show what sealing does?") and explain only after the user says yes.
  Never append the unrequested explanation itself, and never offer the same topic again after a no.
- Consequences, limits and edge cases of an action: state only what the knowledge or tool result of this turn says.
  Never infer or add further consequences (what stays, what disappears, what can be undone). If the result does not
  cover what the user asks, say plainly that you cannot confirm it.
""";

    public const string Header = "=== EXPLANATION DEPTH ===";
    public const string Footer = "=== END EXPLANATION DEPTH ===";

    public const string LevelLineFormat = "Experience level: {0} ({1}).";
    public const string LevelNameBeginner = "beginner";
    public const string LevelNameAdvanced = "advanced";
    public const string SourceExplicitOverride = "chosen by the user";
    public const string SourceEstimated = "estimated from usage";

    public const string BeginnerStyle =
        "Style: explain one concept at a time in plain words — purpose first, then the steps in the visible order. " +
        "Leave edge cases and internal details out unless asked; you may end with one offer sentence for the next step.";

    public const string AdvancedStyle =
        "Style: skip definitions and label explanations. Lead with consequences, what is final versus correctable, " +
        "edge cases and shortcuts or commands. Keep it short and add no offer sentences.";

    public const string PersistentChangeHint =
        "The user can change this level for good by saying e.g. \"always explain more simply\" or \"just the essentials from now on\".";

    public const string ExplicitRequestRule =
        "An explicit request in the user's message for more detail, a simpler or a shorter explanation always beats this style.";
}
