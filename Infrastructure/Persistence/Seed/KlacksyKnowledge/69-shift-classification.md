---
name: explain_shift_classification
description: |
  Explains how Klacks decides whether a shift counts as early, late or night: the three time bands,
  and the rule that the whole span decides rather than the start alone, so a shift reaching into a
  later band takes that band. Use this when someone asks why a shift is treated as late or night
  although it starts in the morning, why a non-shift worker cannot be assigned a particular shift,
  or where the boundary between early, late and night lies.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - schicht
    - shift
  de:
    - frühschicht spätschicht nachtschicht
    - wann ist eine schicht nacht
    - schichtart bestimmen
  en:
    - early late night shift
    - when is a shift night
    - shift classification
synonyms:
  de: [wann gilt eine schicht als nachtschicht, warum ist meine frühschicht plötzlich spät, ab wann zählt eine schicht als spätschicht, grenze zwischen früh und spät, warum darf der mitarbeiter diese schicht nicht, schichtart früh spät nacht]
  en: [when does a shift count as a night shift, why is my early shift treated as late, where is the boundary between early and late, why can this employee not take this shift, shift type early late night]
  fr: [quand un service compte comme service de nuit, pourquoi mon service du matin est traité comme soir, limite entre matin et soir, type de service matin soir nuit]
  it: [quando un turno conta come turno di notte, perché il mio turno mattutino è trattato come sera, confine tra mattina e sera, tipo di turno mattina sera notte]
---

# Shift classification — early, late or night

## Core idea (one sentence)

A shift is early, late or night depending on the time bands its **whole span** touches, and the
strongest band wins: **night beats late beats early**.

## The three bands

| Band | Time |
|---|---|
| Early (de: "Frühdienst", en: "early shift", fr: "service du matin", it: "turno mattutino") | 06:00 – 14:59 |
| Late (de: "Spätdienst", en: "late shift", fr: "service du soir", it: "turno serale") | 15:00 – 22:59 |
| Night (de: "Nachtdienst", en: "night shift", fr: "service de nuit", it: "turno notturno") | 23:00 – 05:59 |

## Why the span decides, not the start

A shift that runs into the night IS night work, whatever the roster calls it. The same logic applies
one step earlier: a shift that runs into the late band is a late shift. So the classification looks at
every band the shift touches and takes the strongest one.

The end is **exclusive**: a shift ending exactly at 15:00 stays early, because its last worked minute
is 14:59.

## Examples

| Shift | Bands touched | Result |
|---|---|---|
| 06:00 – 14:00 | early | **Early** |
| 06:00 – 15:00 | early | **Early** (ends exactly on the boundary) |
| 08:00 – 16:00 | early, late | **Late** |
| 05:00 – 13:00 | night, early | **Night** |
| 15:00 – 23:00 | late | **Late** |
| 15:00 – 23:30 | late, night | **Night** |
| 22:00 – 06:00 | late, night | **Night** |

## What the classification is used for

- **Who may work it.** An employee not marked as doing shift work can only be given early shifts.
- **Shift-type wishes.** The EARLY / LATE / NIGHT tokens a planner enters per day are matched
  against this classification.
- **Surcharge estimate during planning.** The planning assistant estimates a night supplement for
  shifts classified as night. The binding surcharge is calculated separately at booking time.

## Not the same as the night surcharge window

The surcharge night window is a separate, configurable setting (default 23:00–06:00). It decides
whether a supplement is due for hours worked. The classification above decides what kind of shift it
is. The two currently line up at 23:00 and 06:00, but they are set independently.

## Related skills

- `explain_shift_type_preferences` — the EARLY / LATE / NIGHT wishes per day
- `explain_planning_assistant` — how the assistant uses the classification
- `explain_surcharge_mode` — how surcharges are calculated
