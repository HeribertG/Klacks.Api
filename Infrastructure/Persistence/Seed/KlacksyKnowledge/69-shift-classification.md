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

A shift is early, late or night depending on the time bands its **whole span** touches, and the strongest band wins: **night beats late beats early**.

<!-- level:short -->

## Level 1 — What is it for?

Klacks sorts every shift into one of three kinds: early, late or night. Early covers the morning up to 14:59, late runs from 15:00 to 22:59, and night covers 23:00 to 05:59. Klacks looks at the whole shift, not only at its start, so a shift that reaches into a later band counts as that later kind. For example, a shift from 08:00 to 16:00 starts in the morning but runs into the afternoon, so it counts as a late shift. The kind decides, among other things, who may work the shift and how a day wish for early, late or night shifts is matched.

<!-- level:elements -->

## Level 2 — Names and where you meet it

### The three bands

| Band | Time |
|---|---|
| Early (de: "Frühdienst", en: "early shift", fr: "service du matin", it: "turno mattutino") | 06:00 – 14:59 |
| Late (de: "Spätdienst", en: "late shift", fr: "service du soir", it: "turno serale") | 15:00 – 22:59 |
| Night (de: "Nachtdienst", en: "night shift", fr: "service de nuit", it: "turno notturno") | 23:00 – 05:59 |

### Examples

| Shift | Bands touched | Result |
|---|---|---|
| 06:00 – 14:00 | early | **Early** |
| 06:00 – 15:00 | early | **Early** (ends exactly on the boundary) |
| 08:00 – 16:00 | early, late | **Late** |
| 05:00 – 13:00 | night, early | **Night** |
| 15:00 – 23:00 | late | **Late** |
| 15:00 – 23:30 | late, night | **Night** |
| 22:00 – 06:00 | late, night | **Night** |

### Where it shows up for you

- **Contract setting.** The contract option de: "Schichtarbeit (Früh-/Spät-/Nachtdienste)" (en: "Shift Work (Early/Late/Night Shifts)") says whether an employee works late and night shifts at all.
- **Day wishes.** The words EARLY, LATE and NIGHT (default words; an administrator can change them) entered for a day in the schedule grid say which kind of shift that person wants or does not want on that day; their negations are -EARLY, -LATE and -NIGHT. Whether a booked shift fits such a wish is decided with the classification above.

<!-- level:effects -->

## Level 3 — How it connects to other parts

### Why the span decides, not the start

A shift that runs into the night IS night work, whatever the roster calls it. The same logic applies one step earlier: a shift that runs into the late band is a late shift. So the classification looks at every band the shift touches and takes the strongest one.

### What the classification is used for

- **Who may work it.** In the planning eligibility check, an employee whose contract is not marked as shift work is only eligible for early shifts. The recovery (replacement) search applies the same gate.
- **Shift-type wishes.** The EARLY / LATE / NIGHT words a planner enters per day are matched against this classification, keyed on the day the shift starts.
- **Planning rules.** Rules that talk about early or late shifts in a row, forbidden transitions or rest after a kind of shift follow this classification for early and late. For night they use something else (see the last section of the pitfalls).
- **Surcharge estimate during planning.** The planning wizard uses the night surcharge rate for a rough surcharge estimate on night shifts; the real surcharge follows the surcharge settings.
- **One source.** One single function decides the kind for all planning engines; none of them keeps a second opinion.

### Not the same as the night surcharge window

The surcharge night window is a separate, configurable setting (default 23:00–06:00; the value is taken from the scheduling rule, then the contract, then the global settings). It decides whether a supplement is due for hours worked. The classification above decides what kind of shift it is. The two currently line up at 23:00 and 06:00, but they are set independently.

## Related skills

- `explain_shift_type_preferences` — the EARLY / LATE / NIGHT wishes per day
- `explain_planning_assistant` — how the assistant uses the classification
- `explain_surcharge_mode` — how surcharges are calculated

<!-- level:pitfalls -->

## Level 4 — Edge cases and consequences

- **The end is exclusive.** A shift ending exactly at 15:00 stays early, because its last worked minute is 14:59. A shift ending at 23:00 does not touch the night band.
- **Past midnight and 24 hours.** An end that is not after the start means the shift runs past midnight. Equal bounds such as 07:00 – 07:00 span a full 24 hours and therefore count as night (owner ruling of 2026-08-19).
- **A non-shift worker loses more than night shifts.** Because the whole span decides, an 08:00 – 16:00 shift is late, so an employee without shift work in the contract is not eligible for it either, although most of it lies in the morning. Only shifts that stay completely inside 06:00 – 14:59 count as early.
- **A wish against a booking is a conflict, not a refusal.** Booking a shift whose kind the day's wish does not allow is saved when you plan by hand, and then shows up as an error-level conflict after the fact. Automatic placement and the replacement search refuse such a candidate instead.
- **Three different meanings of "night".** (1) The classification: fixed bands, whole span, as described here. (2) The surcharge window: configurable, default 23:00–06:00, decides surcharges only. (3) Night in planning rules such as limits on consecutive nights or rest after a night: counted by the overlap with the employee's contractual night window, and only when the overlap exceeds a minimum (a setting for the minimum overlap, default 60 minutes; 0 means from the first minute). Only if no night window is known do the rules fall back to the classification. So a shift can be night in one sense and not in another.
- **Cutting a shift changes the kind.** Each piece is classified by its own span, so a long shift cut into pieces can end up as an early piece, a late piece and a night piece.
