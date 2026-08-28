---
name: explain_klacksy_learning
description: |
  Explains how Klacksy learns: wishes it could not serve are grouped by their wording, counted, and
  only taken up once the same wish recurs (at least three times, or from at least two different
  people). Each recurring wish ends as a learned phrasing, a learned capability, or an open wish
  nobody can serve yet. Use this when the user asks whether Klacksy learns, why it picked the wrong
  thing, what happens to a correction, or how its capabilities grow.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - lernen
  - lernt
  - learning
  - korrektur
  - correction
  - falsche fähigkeit
  - vorschlag
  - fähigkeiten
  - kannst du lernen
synonyms:
  de: [lernen, lernst du, korrektur, korrigieren, falsche fähigkeit, falsches werkzeug, vorschlag, verbesserung, fähigkeiten erweitern, was kannst du]
  en: [learn, learning, correction, wrong capability, wrong tool, suggestion, improvement, extend capabilities, what can you do]
  fr: [apprendre, correction, mauvaise capacité, suggestion, amélioration]
  it: [imparare, correzione, capacità sbagliata, suggerimento, miglioramento]
---

# How Klacksy learns

## Core idea (one sentence)

Klacksy learns from wishes it could not serve — and only from those that come back, because a single
unanswered request is an accident while a recurring one is a gap.

## What counts as such a wish

Three things mark a turn as one:

- Klacksy answered that it cannot do this.
- The very next message contradicts the answer ("no, that was wrong") within a short reactive window.
- Someone explicitly corrects the turn and names the capability that should have run.

Very short messages are ignored, and so is a turn in which a capability actually ran.

## How the wishes are grouped

The wording is normalised — trimmed, lower-cased, whitespace collapsed — and reduced to a short hash.
That hash is the group. The same wish phrased with different capitalisation or spacing lands in the
same group and raises its counters instead of opening a second one. Of the wording itself, only an
excerpt of at most 120 characters is kept; the full message is never stored.

## When a group is taken up

Once a group has been seen **at least three times**, or by **at least two different people**, it is
marked as ready. Both thresholds are settings and can be raised or lowered without a new release.
Below them nothing happens at all — repetition is the whole evidence.

## The three outcomes

- **A learned phrasing** — the capability already exists, it was only described in words nobody uses.
  The new wording is added to that capability.
- **A learned capability** — no single capability covers the wish, but existing ones can be composed
  into one that does.
- **An open wish** — nothing existing covers it. It stays on the list as evidence of what is genuinely
  missing, which is exactly what belongs in a development decision.

## Where an administrator sees it

In the settings, card **"Klacksy learns"**. It shows the three lists, lets a learned phrasing be
edited or withdrawn, a learned capability be adjusted or switched off, and an open wish be discarded.
A discarded wish never comes back, even if the same sentence is said again. This requires
administrator rights, and Klacksy has no capability of its own for this card — an assistant that could
edit its own learning results could reinforce itself.

## The weekly digest

Once a week Klacksy reports how much was learned in that period and how many wishes are still open,
as a message in the inbox that links to the card. It is a badge, not a push.

## An important limit

Learning changes **wording and composition**, never permissions and never what a capability is
allowed to do. Better wording makes Klacksy choose better; it does not give it new powers.

## Related skills

- `explain_navigation_learning` — the same idea for page navigation
- `list_agent_skills` — what capabilities exist right now
- `create_agent_skill` / `update_agent_skill` / `delete_agent_skill` — add, refine or disable one

## Trigger phrases

- "Do you actually learn from this?"
- "You picked the wrong thing again."
- "What happens when I correct you?"
- "Why do you not know that yet?"
- "Wie erweitere ich deine Fähigkeiten?"
