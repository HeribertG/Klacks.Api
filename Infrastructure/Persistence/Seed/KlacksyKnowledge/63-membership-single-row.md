---
name: explain_membership_single_row
description: |
  Explains that a person's membership is a single row rather than a history: a departure and a
  later return overwrite the same entry's dates instead of adding a new one. Also covers the
  membership's own type field, stored and displayed but never evaluated anywhere — the value that
  actually governs a person's classification lives elsewhere. Use this when asked whether a
  re-entry keeps the old start date, or why changing that field has no effect.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - membership
  de:
    - mitgliedschaft
    - wiedereintritt
  en:
    - membership
    - re-entry
synonyms:
  de: [mitgliedschaft hat keine historie, wiedereintritt überschreibt eintrittsdatum, alte mitgliedschaft geht verloren, mitgliedschaft typ ohne wirkung, nur eine mitgliedschaftszeile pro person, mitgliedschaft wird überschrieben statt neu]
  en: [membership has no history, re-entry overwrites entry date, old membership period lost, membership type field has no effect, only one membership row per person]
  fr: [adhésion sans historique, réintégration écrase la date d'entrée, type d'adhésion sans effet réel]
  it: [appartenenza senza storico, rientro sovrascrive la data di ingresso, tipo di appartenenza senza effetto]
---

# Membership — one row, no history, and a type field that does nothing

## Core idea (one sentence)

A person has exactly one membership record, never a list of past periods, so a later return
overwrites the same entry instead of adding to it.

## No history, by design

Where an address keeps every past version, a membership does not: there is only ever a single
membership per person, holding one start date and one optional end date. Recording somebody's exit
sets the end date on that same record; a later re-entry has nowhere else to go but the same record
too — its start date simply gets replaced. Even a removed membership still occupies that one slot
for the person, so nothing frees up a second entry to preserve the earlier period.

**Example:** Anna joins on 1 March 2023, leaves on 30 June 2024, and rejoins on 1 September 2025.
After the rejoin, her membership shows a start date of 1 September 2025 — the original March 2023
date is gone, not archived anywhere. If the exact history of her first period matters later, it has
to have been recorded outside the membership itself before the rejoin overwrote it.

## A type field that looks meaningful but isn't

The membership record also carries its own type value. It can be set and it is shown back — but no
scheduling rule, report or filter anywhere in Klacks actually reads it to decide anything. The field
that really determines whether somebody counts as staff, external staff or a customer is the
person's own classification, a separate value entirely.

**The confusing part:** the control for changing that real classification sits, visually, inside the
membership section of the edit screen, right next to the membership dates — which makes it easy to
assume it edits the membership's type. It does not; it edits the person's classification directly,
leaving the membership's own type field untouched either way.

## Related skills

- `list_client_memberships` / `update_membership` / `end_client_membership` — reading and changing
  the one membership record
- `update_client_type` — the classification that actually matters

## Trigger phrases

- "Wenn jemand wieder eintritt, sehe ich dann noch das alte Eintrittsdatum?"
- "Ich habe den Mitgliedschafts-Typ geändert — warum ändert sich sonst nichts?"
- "Does re-joining keep the previous membership period?"
- "What is the difference between the membership type and the person's type?"
