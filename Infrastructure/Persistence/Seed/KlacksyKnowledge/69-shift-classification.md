---
name: explain_shift_classification
description: |
  Explains how Klacks decides whether a shift counts as early, late or night: the three time bands,
  and the rule that the whole span decides rather than the start alone. A stay of up to 60 minutes
  in a later band is tolerated, a longer stay (or half of the shift or more) makes the shift take
  that band. Use this when someone asks why a shift is treated as late or night although it starts
  in the morning, why a shift until 18:00 still counts as early, why a non-shift worker cannot be
  assigned a particular shift, or where the boundary between early, late and night lies.
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

A shift is early, late or night depending on the time bands its **whole span** lies in. The stronger band wins (**night beats late beats early**), but a few minutes in a stronger band do not count: up to **60 minutes**, and less than half of the shift, are tolerated.

<!-- level:short -->

## Level 1 — What is it for?

Klacks sorts every shift into one of three kinds: early, late or night. Early covers 06:00 to 17:00, late runs from 17:01 to 22:59, and night covers 23:00 to 05:59. Klacks looks at the whole shift, not only at its start. A shift that stays in a later band for up to 60 minutes (and for less than half of its length) keeps its kind; only a longer stay turns it into the later kind. For example, a shift from 08:00 to 16:00 is an early shift, a shift from 09:00 to 18:00 stays an early shift because it is only 59 minutes into the late band, while a shift from 13:00 to 19:00 is a late shift. The kind decides, among other things, who may work the shift and how a day wish for early, late or night shifts is matched.

<!-- level:elements -->

## Level 2 — Names and where you meet it

### The three bands

| Band | Time |
|---|---|
| Early (de: "Frühdienst", en: "early shift", fr: "service du matin", it: "turno mattutino") | 06:00 – 17:00 |
| Late (de: "Spätdienst", en: "late shift", fr: "service du soir", it: "turno serale") | 17:01 – 22:59 |
| Night (de: "Nachtdienst", en: "night shift", fr: "service de nuit", it: "turno notturno") | 23:00 – 05:59 |

These bands apply since the owner decision of 2026-11-11, because they match what people feel is early, late and night; the former bands (early up to 14:59, late from 15:00) were three times eight hours and too mathematical. Until then a shift from 08:00 to 16:00 counted as late, now it is early.

### The tolerance of 60 minutes

Also an owner decision of 2026-11-11: a few minutes into another band should not flip the kind. Klacks counts the worked minutes per band. A stay in a stronger band is tolerated when it lasts at most 60 minutes **and** is less than half of the shift; otherwise the stronger band wins (night first, then late). A shift from 09:00 to 18:00 is therefore an early shift. The tolerance applies to the night band as well: a shift from 05:00 to 13:00 has exactly 60 night minutes and is an early shift, a shift from 14:00 to 00:00 is a late shift. The reason: the three kinds are an internal planning classification. Legal limits are kept in the settings and act there as hard limits on the real clock times (Klacks is distributed internationally), so they do not depend on this classification.

### Examples

| Shift | Minutes in the stronger band | Result |
|---|---|---|
| 06:00 – 14:00 | none | **Early** |
| 08:00 – 16:00 | none | **Early** (late until 2026-11-11) |
| 09:00 – 18:00 | 59 late | **Early** (tolerated) |
| 09:00 – 18:01 | 60 late | **Early** (tolerated, exactly 60) |
| 09:00 – 18:02 | 61 late | **Late** |
| 13:00 – 19:00 | 119 late | **Late** |
| 17:30 – 18:30 | all late | **Late** |
| 15:00 – 23:00 | none in night | **Late** |
| 05:00 – 13:00 | 60 night | **Early** (tolerated, exactly 60) |
| 04:59 – 13:00 | 61 night | **Night** |
| 14:00 – 00:00 | 60 night | **Late** (tolerated, late dominates) |
| 15:00 – 00:30 | 90 night | **Night** |
| 22:30 – 00:00 | 60 night, but half of the shift | **Night** |
| 22:00 – 06:00 | many | **Night** |

### Where it shows up for you

- **Contract setting.** The contract option de: "Schichtarbeit (Früh-/Spät-/Nachtdienste)" (en: "Shift Work (Early/Late/Night Shifts)") says whether an employee works late and night shifts at all. Left empty, the planning rule and then the company default decide; the company default is "yes" out of the box. While it is effectively "no", only early shifts are possible and the contract's own surcharge rates are ignored.
- **Day wishes.** The words EARLY, LATE and NIGHT (default words; an administrator can change them) entered for a day in the schedule grid say which kind of shift that person wants or does not want on that day; their negations are -EARLY, -LATE and -NIGHT. Whether a booked shift fits such a wish is decided with the classification above.

<!-- level:effects -->

## Level 3 — How it connects to other parts

### Why the span decides, not the start

A shift that runs substantially into the night IS night work, whatever the roster calls it, and one that runs substantially into the late band is a late shift. So the classification counts the minutes in every band and takes the strongest band whose share is not tolerated. A few minutes (up to 60, and less than half of the shift) are tolerated, because a shift that ends at 18:00 does not feel like a late shift.

### What the classification is used for

- **Who may work it.** In the planning eligibility check, an employee whose contract is not marked as shift work is only eligible for early shifts. The recovery (replacement) search applies the same gate.
- **Shift-type wishes.** The EARLY / LATE / NIGHT words a planner enters per day are matched against this classification, keyed on the day the shift starts.
- **Planning rules.** Rules that talk about early or late shifts in a row, forbidden transitions or rest after a kind of shift follow this classification for early and late. For night they use something else (see the last section of the pitfalls).
- **Surcharge estimate during planning.** The planning wizard uses the night surcharge rate for a rough surcharge estimate on night shifts; the real surcharge follows the surcharge settings.
- **One source.** One single function decides the kind for all planning engines; none of them keeps a second opinion.

### What changes in practice

- **Employees without shift work** get only early shifts. A shift from 05:00 to 13:00 or from 09:00 to 18:00 counts as early now and is allowed for them; a shift from 14:00 to 00:00 counts as late and stays closed to them.
- **Day wishes.** "No night shift" (-NIGHT) now allows 05:00 to 13:00 and 14:00 to 00:00. "Only night shifts" (NIGHT) is no longer met by 05:00 to 13:00. The wishes for early and late follow the same classification: 09:00 to 18:00 meets EARLY and -LATE and is refused by LATE and -EARLY.
- **Planning rules by kind** (at most so many shifts of the same kind in a row, rest after a kind, forbidden transition from one kind to another) see a shift from 05:00 to 13:00 as early now.
- **Not affected:** surcharges, rest times and maximum hours are calculated with the real clock times and do not depend on the classification.

### Not the same as the night surcharge window

The surcharge night window is a separate, configurable setting (default 23:00–06:00; the value is taken from the scheduling rule, then the contract, then the global settings). It decides whether a supplement is due for hours worked. The classification above decides what kind of shift it is. The two currently line up at 23:00 and 06:00, but they are set independently.

## Related skills

- `explain_shift_type_preferences` — the EARLY / LATE / NIGHT wishes per day
- `explain_planning_assistant` — how the assistant uses the classification
- `explain_surcharge_mode` — how surcharges are calculated

<!-- level:pitfalls -->

## Level 4 — Edge cases and consequences

- **The end is exclusive.** The late band begins with the minute 17:01. A shift from 09:00 to 18:01 works its last minute at 18:00, which is the 60th minute of the late band, and stays early; a shift from 09:00 to 18:02 has 61 late minutes and is late. A shift starting at 17:00 and running into the evening is late, because nearly all of it lies in the late band. A shift ending at 23:00 has no night minutes.
- **Half of the shift counts as well.** The tolerance applies only while the stay is less than half of the shift. A short shift from 22:30 to 00:00 has 60 night minutes, but that is the larger part of it, so it is night. A shift from 16:30 to 17:45 has 44 late minutes of 75 and is late.
- **Past midnight and 24 hours.** An end that is not after the start means the shift runs past midnight. Equal bounds such as 07:00 – 07:00 span a full 24 hours and therefore count as night (owner ruling of 2026-08-19).
- **A non-shift worker may take what counts as early.** An employee without shift work in the contract is eligible only for early shifts. A 09:00 – 18:00 shift counts as early, so such an employee can work it; a 09:00 – 18:02 shift is late and is closed to them.
- **A wish "no late shift" does not protect against 18:00.** Because 09:00 – 18:00 is an early shift, it can be assigned to someone with the wish -LATE (no late shift), and it is refused for someone with the wish -EARLY. If the end of the day matters to a person, express it through availability or a rule instead of the wish alone.
- **Existing shifts can change sides.** The kind is not stored; it is worked out from the times every time. Since 2026-11-11 a daytime shift that used to count as late can count as early, for example 08:00 – 16:00 and 09:00 – 18:00, and a shift that starts up to 60 minutes before 06:00 and has its larger part in the day, for example 05:00 – 13:00, counts as early instead of night. A day wish LATE or -EARLY on such a day, or a non-shift worker on such a shift, is therefore judged differently than before.
- **A wish against a booking is a conflict, not a refusal.** Booking a shift whose kind the day's wish does not allow is saved when you plan by hand, and then shows up as an error-level conflict after the fact. The replacement search refuses such a candidate; the automatic planning avoids it and breaks such a wish only in a second run to fill otherwise open shifts (see `explain_shift_type_preferences`).
- **Three different meanings of "night".** (1) The classification: fixed bands, whole span with a tolerance of 60 minutes, as described here. (2) The surcharge window: configurable, default 23:00–06:00, decides surcharges only. (3) Night in planning rules such as limits on consecutive nights or rest after a night: counted by the overlap with the employee's contractual night window, and only when the overlap exceeds a minimum (a setting for the minimum overlap, default 60 minutes; 0 means from the first minute). Only if no night window is known do the rules fall back to the classification. So a shift can be night in one sense and not in another.
- **Cutting a shift changes the kind.** Each piece is classified by its own span, so a long shift cut into pieces can end up as an early piece, a late piece and a night piece.
