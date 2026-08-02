---
name: explain_absence_blocks_planning
description: |
  Explains how a booked absence affects the automatic scheduling assistants. A booked absence is
  always untouchable for the wizards, unlike a work shift whose untouchability depends on its lock
  stage. Its hours count towards a person's target hours but never towards the weekly hours cap, and
  it interrupts a run of consecutive working days. Use this when asking whether the planner can move
  a booked holiday or why a sick week caused no overtime warning.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - harmonizer
  de:
    - wochenmaximum absenz
    - absenz unterbricht tage-serie
  en:
    - weekly hours cap absence
    - absence interrupts consecutive run
synonyms:
  de: [blockiert eine absenz die planung, verschiebt der assistent eine gebuchte absenz, zählt absenz zum wochenmaximum, unterbricht absenz die tage-serie, ist eine gebuchte absenz gesperrt für die planung, warum wurde meine krankheit nicht verschoben, darf der planer ferien verschieben]
  en: [does a booked absence block planning, can the wizard move a booked vacation, does absence count toward the weekly cap, does absence interrupt the consecutive run, is a booked absence locked for the planner, why was my sick day not moved]
  fr: [une absence bloque-t-elle la planification, l assistant peut-il déplacer une absence réservée, l absence compte-t-elle dans le maximum hebdomadaire, l absence interrompt-elle la série de jours]
  it: [un assenza prenotata blocca la pianificazione, l assistente può spostare un assenza prenotata, l assenza conta nel massimo settimanale, l assenza interrompe la serie di giorni]
---

# Booked absences and the planning assistants

## Core idea (one sentence)

A booked absence is off-limits for every planning assistant no matter what — a work shift is only
off-limits once it has reached a locking stage.

## Two kinds of "cannot touch this"

A work shift becomes untouchable for the assistants only once it has been confirmed, approved or
closed — an unconfirmed shift is still fair game for a rearrangement. A booked absence has no such
staging: the moment it exists, every assistant treats it as fixed, regardless of any lock state on
it. Vacation, sickness, training — none of it is ever shuffled around automatically.

## What that means for the hour counts

Hours from a booked absence count towards the target hours a person owes for the period — someone on
holiday still moves towards their contracted hours as if they had worked. They do **not** count
towards the weekly hours ceiling: a sick week does not push a person over their weekly maximum, and
does not trigger an overtime rejection the way an unusually long run of shifts would.

## What that means for consecutive-day rules

A limit on how many days in a row somebody may work treats a booked absence like a day off: the
count of consecutive working days resets there. Three working days, a day of vacation, then three
more working days is **not** a run of six — the absence breaks it, so a rule capping runs at, say,
five days is not violated even though nine calendar days are covered end to end.

## Related skills

- `add_break`, `update_break`, `delete_break` — placing the booked absence itself
- `cover_absence` — find replacement cover once an absence is booked

## Trigger phrases

- "Verschiebt der Planungs-Assistent eine gebuchte Absenz?"
- "Does a sick week count against the weekly hours limit?"
- "Zählt eine Ferienwoche gegen die Sechs-Tage-Regel?"
- "Why didn't the wizard reschedule this vacation?"
