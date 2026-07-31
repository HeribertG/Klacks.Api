---
name: explain_klacksy_personality
description: |
  Explains how Klacksy's character is configured: six free-text sections describing identity, drive,
  tone, boundaries, communication style and values, plus the separate guidelines that constrain its
  behaviour. Use this when the user asks why Klacksy sounds the way it does, wants it friendlier,
  shorter or more formal, asks who decides its character, or wants to know what it is forbidden to
  do.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - persönlichkeit
  - personality
  - charakter
  - tonfall
  - tone
  - verhalten
  - wer bist du
  - leitlinien
  - guidelines
synonyms:
  de: [persönlichkeit, charakter, wesen, tonfall, ton, umgangston, verhalten, wer bist du, leitlinien, regeln, grenzen, freundlicher, kürzer]
  en: [personality, character, tone, voice, behaviour, who are you, guidelines, boundaries, be shorter, be friendlier]
  fr: [personnalité, caractère, ton, comportement, qui es-tu, directives, limites]
  it: [personalità, carattere, tono, comportamento, chi sei, linee guida, limiti]
---

# Klacksy's character — configured, not hard-wired

## Core idea (one sentence)

Klacksy has no fixed built-in character: its behaviour is described in six free-text sections that
anyone with administrator rights can rewrite in their own words.

## The six sections

- **Identity** — who Klacksy is: role, purpose, context.
- **Personality and drive** — character, basic attitude, what motivates it.
- **Tone** — formal or casual, terse or expansive.
- **Boundaries** — what Klacksy must not do: privacy, off-limits topics, delicate areas.
- **Communication style** — how answers are built, how uncertainty is handled, when to ask back.
- **Values and continuity** — guiding values and how Klacksy develops over time.

Each field is plain free text with no formatting rules. Short, clear sentences work more reliably
than long flourishes. An empty field is not a problem — Klacksy falls back to its built-in default
for that aspect. A few further behavioural aspects are maintained by the system itself and are not
editable here.

## Two things worth knowing before editing

**A change affects everyone.** The character belongs to the shared assistant, not to one account.
Unlike the autonomy level, this is not a personal setting — rewriting the tone changes it for every
user of the installation.

**Saving is immediate.** Each field saves on its own as soon as it loses focus; there is no separate
save button. The next message to Klacksy already uses the new text.

## Guidelines are separate

Alongside the character there are guidelines: rules Klacksy has to follow regardless of how its
personality is worded. Replacing the guidelines deactivates all previous ones at once rather than
adding to them — so they are read first and rewritten whole, never patched blindly.

## Related skills

- `get_ai_soul` / `update_ai_soul` — read and rewrite a character section
- `get_ai_guidelines` / `update_ai_guidelines` — read and replace the guidelines

## Trigger phrases

- "Why do you talk like that?"
- "Answer me more briefly from now on."
- "Who decides how you behave?"
- "What are you not allowed to do?"
- "Kannst du etwas lockerer schreiben?"
