---
name: explain_klacksy_skill_relations
description: |
  Explains what the assistant notices about which of its abilities belong together: pairs used in
  the same conversation, and pairs where one regularly follows the other. Covers the number
  expressing how sure it is, how confirming or rejecting a suggestion moves that number, why a
  rejected suggestion can reappear, and what these pairs change at runtime — quietly offering a
  companion ability, and proposing the usual next step. Use this when the user asks what the
  assistant learns from watching, or what confirming an observation does.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  de:
    - skill-beziehung
    - beziehungen zwischen fähigkeiten
    - konfidenz
    - erkenntnis übernehmen
  en:
    - skill relation
    - confidence
    - accept insight
synonyms:
  de: [skill-beziehung, beziehungen zwischen fähigkeiten, was hat klacksy bemerkt, konfidenz, erkenntnis übernehmen, erkenntnis verwerfen, warum kommt der vorschlag wieder, gelernte verknüpfung]
  en: [skill relation, what has klacksy noticed, confidence, accept insight, dismiss insight, why does the suggestion come back, learned connection]
  fr: [relation entre skills, ce que klacksy a remarqué, confiance, accepter une observation, rejeter une observation]
  it: [relazione tra skill, cosa ha notato klacksy, affidabilità, accettare osservazione, rifiutare osservazione]
---

# What the assistant notices about its own abilities

<!-- level:short -->

## Stage 1 — What this is for

While working, the assistant watches which of its abilities tend to appear together. Out of that it
forms pairs of two kinds:

- **Needed together** (de: "Gemeinsam nötig") — both turn up in the same conversation, in no
  particular order.
- **One after the other** (de: "Aufeinanderfolgend") — the second regularly follows the first.

Each pair carries a **confidence** (de: "Konfidenz"), a number saying how sure the assistant is. It
rises slowly on repeated evidence and falls quickly on contradiction — deliberately asymmetric, so
that a pattern seen a few times by accident does not stick.

The card (de: "Skill-Beziehungen") shows only what is still **undecided** and asks you to judge it.

<!-- level:elements -->

## Stage 2 — The card

Card anchor: `settings-assistant-skill-relations`.

Each entry shows the two abilities in plain words, the kind of pair, the confidence, how often it
has been confirmed (de: "Bestätigungen"), how often contradicted (de: "Widersprüche"), and how the
assistant came to notice it (de: "Wie Klacksy es bemerkt hat").

**Two actions, and no delete:**

- **Accept** (de: "Übernehmen") raises the confidence and counts as a confirmation.
- **Dismiss** (de: "Verwerfen") lowers it and counts as a contradiction.

**A judged entry does not necessarily leave the list.** Accepting or dismissing only moves the
number; whether the entry is settled depends on whether it crosses the threshold for its kind — and
the threshold for the "one after the other" kind is higher. The row disappears from the current view
immediately, but it can be back the next time the card is opened. That is working as designed, not a
fault.

**On a fresh installation the list is virtually empty.** Nearly everything that ships is already
decided. Observations of your own appear once there is enough to observe: the assistant looks back
over the past month, needs a handful of usable conversations before it forms any opinion at all,
requires each ability to have been seen several times, and re-examines things every few hours.

<!-- level:effects -->

## Stage 3 — What the pairs change

**Offering a companion ability.** When the assistant has picked what it needs for your request and
has room left over, it may quietly add a companion from a settled "needed together" pair. It never
displaces anything it had already chosen, adds at most a few, and now and then skips this on purpose
so that what it learns next is not merely an echo of what it did before.

**Proposing the next step.** After an ability has been used successfully, a settled "one after the
other" pair lets the assistant offer the usual follow-up. That proposal goes through the same
politeness rules as any other — your preferences and the limit on how often it may speak up
unprompted.

Undecided pairs do neither. Only settled ones take effect, which is what confirming and rejecting is
for.

Pairs are never deleted, only weakened until they fall out of use.

## Related skills

- `list_skill_relations`, `accept_skill_relation`, `dismiss_skill_relation`

## Trigger phrases

- "Was hat Klacksy über seine Fähigkeiten gelernt?"
- "Was passiert, wenn ich eine Erkenntnis übernehme?"
- "Ich habe das verworfen, es taucht wieder auf."
- "Die Liste ist leer — ist das kaputt?"
- "Beeinflusst das, welche Werkzeuge er benutzt?"
