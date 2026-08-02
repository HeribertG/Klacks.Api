---
name: explain_absence_overlap_not_blocked
description: |
  Explains whether Klacks stops a person from ending up with two overlapping absence entries.
  Pre-planned wishes are never checked against each other or against booked absences, so overlapping
  requests of any kind can both be saved. Booking directly has no such check either; only the
  assistant's own route refuses an exact repeat of the same person, day and kind — a different kind
  on the same day still goes through. Use this for double-booked or clashing absences.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - duplicate
  de:
    - überlappende absenz
    - doppelte absenz
  en:
    - overlapping absence
    - duplicate absence
synonyms:
  de: [kann ich zwei überlappende ferien anlegen, verhindert der server doppelte absenzen, wird eine überschneidende absenz abgelehnt, krank und ferien am gleichen tag, prüft klacks überlappende abwesenheiten, zwei absenzen am selben tag für dieselbe person]
  en: [can i create two overlapping vacations, does the server prevent duplicate absences, is an overlapping absence rejected, sick and vacation on the same day, does klacks check overlapping absences, two absences on the same day for the same person]
  fr: [puis-je créer deux congés qui se chevauchent, le serveur empêche-t-il les absences en double, une absence chevauchante est-elle refusée, maladie et congé le même jour]
  it: [posso creare due ferie sovrapposte, il server impedisce assenze doppie, un assenza sovrapposta viene rifiutata, malattia e ferie lo stesso giorno]
---

# Overlapping absences mostly go through unblocked

## Core idea (one sentence)

For almost every way of entering an absence, Klacks does not check whether the same person already
has an overlapping entry — the one narrow exception blocks only an exact repeat, not a real overlap.

## Pre-planned wishes: no check at all

Entering a pre-planned absence wish only requires a valid person, a valid absence kind, and dates
inside that person's membership period. Nothing compares it against other wishes or against absences
already booked for the same person. Two overlapping vacation requests, or a wish that sits on top of
an already-booked sick day, are both accepted without complaint.

## Booking an absence directly: also no check

Booking an absence the way it is normally entered, independent of the assistant, carries no overlap
rule either. Two absences for the same person on the same day, even of the same kind, are both saved.

## Booking through the assistant: one narrow exception

There is exactly one place a duplicate is refused: when the assistant itself places an absence, it
is rejected if the same person already has an absence of the **same kind** overlapping the **same
day**. Anything short of an identical repeat slips through — a different kind of absence that day
(sickness during an already-booked vacation, say) is booked without objection.

## What actually catches this

A dedicated feasibility check looks at overlapping wishes, overlapping booked absences, scheduled
work and the membership window all at once — but it is advisory only. It reports what it found; it
does not stop anyone from saving the absence anyway.

## Related skills

- `check_absence_conflicts` — the advisory pre-flight check, run before booking
- `add_break`, `add_break_placeholder` — the booking paths this happen describes

## Trigger phrases

- "Kann ich für dieselbe Person zwei sich überschneidende Ferienwünsche anlegen?"
- "Does the server block a duplicate sick day on top of a vacation?"
- "Verhindert Klacks doppelte Absenzen für dieselbe Person?"
- "Why was I able to book vacation and sick leave on the same day?"
