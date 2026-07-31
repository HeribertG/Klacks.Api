---
name: explain_klacksy_learning
description: |
  Explains how Klacksy improves at picking the right capability: corrections in the chat are turned
  into proposed wording changes that an administrator accepts or discards, and detected gaps surface
  where no capability exists yet. Use this when the user asks whether Klacksy learns, why it picked
  the wrong thing, how a correction is used, or how the list of capabilities is extended.
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

# How Klacksy gets better at choosing

## Core idea (one sentence)

Klacksy learns from being corrected, not from being used — and no correction changes anything until
a person approves it.

## Where suggestions come from

When Klacksy picks the wrong capability for a request and the user corrects it in the chat, that
exchange is marked as corrected. An internal optimiser reviews such corrected exchanges and derives
concrete wording changes to the descriptions of capabilities, so that the same request lands
correctly next time. Reviewing the most recent corrected exchanges can also be triggered by hand.

Without corrections there are no suggestions. Heavy use alone produces nothing — the system needs
real mistakes that someone bothered to correct.

## What a suggestion shows

The affected capability and the field being changed, the wording before and after, a reason why the
change should help, and — expandable — the actual user requests that triggered it.

Each open suggestion is either **accepted**, which changes the description immediately, or
**discarded**, which changes nothing. There is no automatic adoption; a human always decides. This
requires administrator rights.

## An important limit

This only changes the **description** of a capability so Klacksy selects it correctly. It never
changes what the capability actually does. Better wording makes Klacksy choose better — it does not
give it new powers.

## Detected gaps

Separately, Klacksy notices requests it has no capability for at all, and records how often each one
came up. That list is where genuinely missing functionality shows up, as opposed to functionality
that exists but was described badly.

## Capabilities can also be added directly

An administrator can register a new capability at runtime and it becomes usable immediately, or
disable one that should no longer be offered. Only capabilities created this way can be removed
again — the ones built into Klacksy stay.

## Related skills

- `review_skill_suggestions` — review detected gaps and pending suggestions, accept or discard
- `list_agent_skills` — what capabilities exist right now
- `create_agent_skill` / `update_agent_skill` / `delete_agent_skill` — add, refine or disable one

## Trigger phrases

- "Do you actually learn from this?"
- "You picked the wrong thing again."
- "What happens when I correct you?"
- "Can I teach you something new?"
- "Wie erweitere ich deine Fähigkeiten?"
