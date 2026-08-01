---
name: explain_overtime_model
description: |
  Explains how extra hours are paid once somebody works beyond a threshold: up to three bands, each
  starting at a number of hours accumulated in the day or in the week and carrying its own uplift.
  Covers that each band pays only the hours falling inside it rather than everything from zero, that
  the reference span is a day or a week and nothing longer, and the two conditions without which no
  uplift is produced at all. Use this when the user asks from which hour extra pay starts, why an
  amount looks too small, or why nothing was added despite a long day.
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
    - überstunden
    - überstundenzuschlag
    - stufe ab stunden
    - mehrarbeit
  en:
    - overtime
    - overtime tier
    - extra hours pay
synonyms:
  de: [überstunden, überstundenzuschlag, mehrarbeit, ab wann überstunden, überstundenstufe, stufe 1 ab stunden, zuschlag pro stunde, warum wurden keine überstunden berechnet]
  en: [overtime, overtime surcharge, overtime tier, from which hour is overtime paid, extra hours, why was no overtime calculated]
  fr: [heures supplémentaires, majoration heures supplémentaires, palier, à partir de quelle heure, pourquoi pas de majoration]
  it: [straordinari, maggiorazione straordinari, livello, da quale ora, perché nessuno straordinario]
---

# Overtime — bands, thresholds and the two conditions

<!-- level:short -->

## Stage 1 — What this is for

Extra hours are paid in **up to three bands** (de: "Überstunden", en: "Overtime",
fr: "Heures supplémentaires", it: "Straordinari"). Each band starts at a number of hours
accumulated within the reference span and carries its own uplift.

Two things decide everything:

- **The reference span** (de: "Berechnungsbasis") — a **day** or a **week**. There is nothing
  longer; a monthly or yearly basis does not exist.
- **The bands** — up to three pairs of "from this many hours" and "this much uplift".

<!-- level:elements -->

## Stage 2 — How a band is defined and what it pays

Card anchor: `overtime-settings-container`; the three bands sit at
`overtime-settings-tier1-row`, `-tier2-row`, `-tier3-row`.

Each band has two fields:

- **From hours** (de: "Stufe 1 ab Stunden", en: "Tier 1 after hours") — the accumulated hours in
  the day or week from which this band applies.
- **Uplift** (de: "Stufe 1 Zuschlag", en: "Tier 1 rate") — entered as a percentage.

**Each band pays only the hours that fall inside it.** A band does not apply to everything from
zero. With bands starting at 8 and at 10 hours, a ten-and-a-half-hour day pays the first band for
the two hours between 8 and 10, and the second band for the half hour above 10 — never the second
band for all ten and a half.

The last band is open-ended upwards. A single stretch of work can therefore produce several
entries, one per band it reaches into.

**A band with no starting hour, or with an uplift of zero or less, is silently skipped.** Only
bands that are complete take effect, so zero to three of them are actually live.

**The uplift is the surcharge portion, not the total.** An entered 25 % produces a quarter of an
hour's worth per hour worked in that band — it does not mean the hour is paid at 1.25 times. The
mode field beside it (de: "Zuschlagsmodus") switches the **unit** between a multiple of the hourly
value and a fixed amount per hour; the arithmetic is identical either way.

## What counts as hours already worked

The bands are filled by the hours worked earlier in the same day or week — earlier meaning by date,
then start time. That way each stretch of work occupies its own slice of the span and no hour is
counted into an upper band twice. When an earlier entry changes, the later ones are recalculated.

The week begins on the configured first day of the week.

<!-- level:effects -->

## Stage 3 — Where the values come from, and when nothing happens

The figures are taken from the first source that supplies a complete first band:

1. the rule preset attached to the person's working conditions
2. a dated revision of that preset, if one applies to the date — it replaces the whole set; a
   revision without an overtime block falls back to the company settings, not to the preset
3. the company-wide overtime settings — the card described here
4. only when no starting hour is configured at all: the overtime threshold from the working
   conditions, and then only as the first band's starting hour, never as an uplift

Sources are never mixed. Whichever supplies the first band supplies all of them.

**Two conditions without which no uplift is ever produced — this is the usual reason for
"why is there nothing?":**

- **The shift needs a calculation macro.** Work on a shift without one is never examined for extra
  hours at all.
- **At least one complete band must be configured.** With none, the result is not "zero overtime"
  but no examination.

Corrections and stand-ins are deliberately excluded; only regular work is examined.

## Meeting other uplifts on the same hour

The same hour can qualify both for extra hours and for a circumstance-based uplift such as night or
weekend work. Which of the two applies is not decided here — it follows from the calculation macro
of that shift: either both are added together, or the higher of the two replaces the other.

## Related skills

- `get_overtime_settings`, `update_overtime_settings`

## Trigger phrases

- "From which hour do extra hours get paid?"
- "Warum wurde für den langen Dienst nichts zugeschlagen?"
- "Zählt das pro Tag oder pro Woche?"
- "Gilt der Satz für alle Stunden oder nur für die über der Grenze?"
- "Can I set a monthly overtime threshold?"
