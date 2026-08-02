---
name: explain_absence_value_vs_hours
description: |
  Explains why the VALUE shown for an absence entry in the calendar list is not the number of hours
  that later counts, even once booked. The displayed value is a plain days-times-default-rate figure
  that never excludes weekends or holidays, while the stored hours come from a separate calculation:
  a macro on the absence kind when one exists, or otherwise the value given at booking time. Use this
  when a displayed figure does not match the booked hours.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - makro
  de:
    - wert stimmt nicht mit stunden überein
    - woher kommen die absenz-stunden
  en:
    - value does not match hours
    - where do absence hours come from
synonyms:
  de: [wert spalte stimmt nicht mit den stunden überein, warum zeigt die liste eine andere zahl, rechnet der kalender wochenenden heraus, woher kommen die tatsächlichen stunden einer absenz, berechnet ein makro die absenz-stunden, stimmt der angezeigte wert mit der lohnabrechnung überein]
  en: [the value column does not match the hours, why does the list show a different number, does the calendar subtract weekends from the value, where do the actual absence hours come from, does a macro calculate the absence hours, does the displayed value match payroll]
  fr: [la colonne valeur ne correspond pas aux heures, pourquoi le calendrier affiche un autre nombre, le calendrier déduit-il les jours fériés de la valeur]
  it: [la colonna valore non corrisponde alle ore, perché il calendario mostra un numero diverso, il calendario sottrae i giorni festivi dal valore]
---

# The calendar VALUE and the booked hours are two different numbers

## Core idea (one sentence)

The number shown in the absence calendar's VALUE column and the number of hours actually stored on
that same entry are computed independently, by two calculations that never talk to each other.

## The displayed value

The VALUE column shows, for every row — pre-planned wish or already-booked absence alike — the number
of calendar days in the entry (inclusive of both ends) multiplied by the absence kind's default rate.
It is computed on the fly for display only, is never written back, and does **not** subtract weekends
or public holidays: a five-day entry over a weekend is still "5 × rate", not "3 × rate" even though
only three of those days would be worked otherwise.

## The stored hours

Once an absence is booked, its hours follow a different rule entirely, independent of the displayed
value. Every booking path runs the same check: if the absence kind has a calculation macro attached,
the macro computes the hours from the day's real context — contract, weekday, holiday — and
overwrites whatever value the booking started with. If the kind has no macro, the hours simply stay
whatever was supplied at booking time (the assistant defaults that to a flat number of hours per day
unless told otherwise). No booking path skips this check; it just has nothing to do without a macro.

## The surprise

The calendar list keeps showing the days-times-rate VALUE even for an already-booked row, so the
number on screen and the hours actually booked can permanently disagree for that very same entry —
one is a display convenience, the other is what payroll sees.

## Related skills

- `add_break`, `update_break` — booking, where the macro (if any) computes the real hours
- `list_absence_types` — shows an absence kind's default rate and whether it has a macro

## Trigger phrases

- "Warum stimmt der Wert in der Liste nicht mit den Lohnstunden überein?"
- "Does the calendar figure already exclude weekends?"
- "Woher kommen die tatsächlichen Stunden einer gebuchten Absenz?"
- "Why does a booked absence show one number but pay a different one?"
