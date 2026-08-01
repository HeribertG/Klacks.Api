---
name: explain_individual_periods
description: |
  Explains freely defined accounting spans: a named set of rows, each with a start date, an
  optional end date and the hours owed for that span, selectable on working conditions that are
  settled on an individual rhythm rather than weekly, fortnightly or monthly. Covers that rows may
  overlap on purpose, which row wins when several match a date, and what the hours figures do and
  do not affect today. Use this when the user asks about a settlement rhythm that follows neither
  the calendar month nor a fixed number of weeks.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - individuelle periode
    - eigene abrechnungsperiode
    - abweichender zahlungsrhythmus
  en:
    - individual period
    - custom accounting period
synonyms:
  de: [individuelle periode, eigene abrechnungsperiode, abweichender zahlungsrhythmus, periodendefinition, von bis volle stunden, abrechnung nicht monatlich]
  en: [individual period, custom accounting period, own settlement period, period definition, from until full hours, settlement not monthly]
  fr: [période individuelle, période de décompte personnalisée, rythme de règlement différent, définition de période]
  it: [periodo individuale, periodo di conteggio personalizzato, ritmo di liquidazione diverso, definizione del periodo]
---

# Individual periods — freely defined accounting spans

## Core idea (one sentence)

A named list of spans — each with a start, an optional end and the hours owed — for working
conditions that are settled on their own rhythm instead of weekly, fortnightly or monthly.

## Where it is chosen

Working conditions carry a settlement rhythm. Besides weekly, fortnightly, monthly and the
monthly-target-hours table, there is an **individual** rhythm; only that one offers a named period
definition to pick.

## The fields

Card anchor: `individual-periods-card`.

A definition has a **name** and at least one row. Each row
(de: "Von" / "Bis" / "Volle Stunden", en: "From" / "Until" / "Full Hours"):

- **From** — the day the span begins. Required.
- **Until** — the day it ends. May be left empty for an open end.
- **Full hours** — the hours owed for that span. May not be negative.

## Overlapping rows are intended

Only two things are checked: hours may not be negative, and an end may not lie before its start.
Overlaps, gaps and repeated start dates are deliberately allowed.

The reason is corrections: **when several rows match a date, the one with the latest start wins.**
A row added later therefore supersedes what it overlaps, without anyone having to edit the earlier
row. A row with an open end runs until the day before its successor begins.

A definition cannot be deleted while working conditions still refer to it.

## What the hours figures affect today

**They are recorded and validated, but no calculation reads them yet.** Hour balances, period
closing and exports currently derive their spans from the calendar month, including for working
conditions set to the individual rhythm.

So the definitions can be maintained and are kept consistent, but changing a figure here does not
change a balance anywhere. Anybody expecting a different balance from these rows should know that
before relying on them.

## Related skills

- `list_individual_periods`, `create_individual_period`, `update_individual_period`, `delete_individual_period`

## Trigger phrases

- "Wir rechnen nicht monatlich ab — wie bilde ich das ab?"
- "Can two periods overlap?"
- "Welche Zeile gilt, wenn zwei passen?"
- "Ich habe die Stunden geändert, der Saldo bleibt gleich."
