---
name: explain_absence_model
description: |
  Explains the three different things called an absence: the absence type as master data, the actual
  absence booked on a person and a date, and a pre-booked wish that reserves nothing yet. Covers how
  a wish becomes a real absence, that absence hours count towards target hours, and which counting
  rules an absence type carries for weekends and public holidays. Use this when the user asks why a
  requested holiday does not show in the schedule, how to create a new kind of absence, or how
  absences affect the hours balance.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - absenztyp
  - absence type
  - ferienwunsch
  - vorgemerkt
  - urlaubsantrag
  - zählt absenz
synonyms:
  de: [absenztyp, absenzart, ferienwunsch, urlaubsantrag, vorgemerkt, vorgeplante absenz, wird nicht angezeigt, zählt die absenz zu den stunden, neue absenzart anlegen]
  en: [absence type, holiday request, pre-booked, wish, does not show in the schedule, do absences count towards hours, create a new absence kind]
  fr: [type d absence, demande de congé, pré-réservé, n apparaît pas dans le planning, les absences comptent-elles]
  it: [tipo di assenza, richiesta di ferie, pre-prenotato, non appare nel piano, le assenze contano nelle ore]
---

# Absences — three different things with similar names

## Core idea (one sentence)

"Absence" means three separate things in Klacks, and mixing them up is the most common reason a
booked holiday seems to have vanished.

## The three

**The kind of absence** is master data: holiday, sick leave, training, military service. It is
created once and reused. Each kind carries a multilingual name and abbreviation, a colour, a default
duration, and — importantly — **rules for whether it counts on Saturdays, Sundays and public
holidays**. Two kinds with the same name are refused.

**The actual absence** is one kind, booked on one person, on one date. This is what appears in the
schedule and what the hours calculation sees.

**The wish** is a pre-booking over a date range in the absence calendar — a holiday request. It
reserves nothing: it places **no** actual absence in the schedule. A planner turns it into real
absences later.

That third one explains the classic complaint: a holiday request was entered, the absence calendar
shows it, and the schedule shows nothing. Nothing is broken — the request has not been materialised
yet.

## Effect on hours

Hours from an actual absence **count towards the target hours** a person owes. Somebody on holiday
does not fall behind their contract. Which is precisely why the counting rules on the absence kind
matter: whether a Saturday counts, whether a public holiday inside a holiday period is consumed or
not, is decided there and not per booking.

## What Klacksy can check

Beyond booking and deleting, absences can be examined: conflicts for one person, a summary per
person, and overlaps within a group — the "who else is away that week" question that decides
whether a request can be granted.

## Naming trap

The skill that creates a new **kind** of absence is not the one that books an absence for somebody.
Booking works through the absence-placing skill referencing an existing kind. Getting this the wrong
way round creates master data nobody wanted.

## Related skills

- `list_absence_types` / `create_absence_type` / `update_absence_type` / `delete_absence_type` — the kinds
- `add_break` / `update_break` / `delete_break` — the actual absence on a person and date
- `add_break_placeholder` / `update_break_placeholder` / `delete_break_placeholder` — wishes
- `check_absence_conflicts` / `get_client_absence_summary` / `get_group_absence_overlap` — checks
- `cover_absence` — find cover for an absent person

## Trigger phrases

- "The holiday request is not showing in the schedule."
- "Create a new absence kind for further training."
- "Do holidays count towards the hours owed?"
- "Who else is away that week?"
- "Was ist der Unterschied zwischen Wunsch und Absenz?"
