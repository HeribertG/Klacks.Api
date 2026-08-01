---
name: explain_surcharge_mode
description: |
  Explains the company-wide settings behind night, holiday and weekend uplifts: whether a rate is
  read as a multiple, as a fixed amount per hour or as a lump sum per shift, the floor that can be
  put under each one and the three conditions it needs to bite, the hours counted as night, and what
  happens when several uplifts apply to the same stretch of work. Use this when the user asks
  whether uplifts add up or only the largest counts, what a rate figure actually means, or why a
  minimum amount has no effect.
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
    - zuschlagsmodus
    - kumulierung
    - nachtzuschlag modus
    - mindestbetrag pro stunde
    - nachtzeitfenster
  en:
    - surcharge mode
    - stacking
    - minimum per hour
    - night window
synonyms:
  de: [zuschlagsmodus, kumulierung, kumulierungsmodus, addieren sich zuschläge, höchster zuschlag gewinnt, mindestbetrag pro stunde, nachtzeitfenster, was bedeutet der zuschlagswert]
  en: [surcharge mode, stacking mode, do surcharges add up, highest surcharge wins, minimum per hour, night window, what does the rate figure mean]
  fr: [mode de majoration, cumul des majorations, la plus élevée l emporte, minimum par heure, fenêtre de nuit]
  it: [modalità di maggiorazione, cumulo, vince la più alta, minimo all ora, finestra notturna]
---

# Surcharge settings — how a rate figure is read

<!-- level:short -->

## Stage 1 — What this is for

This card (de: "Zuschlagsmodus", en: "Surcharge mode") holds the company-wide decisions behind
night, holiday and weekend uplifts. It does **not** hold the rates themselves — those come from a
person's working conditions or from the rule preset. What is decided here applies to the whole
installation and cannot be varied per person:

- **How a rate figure is read** — as a multiple, as a fixed amount per hour, or as a lump sum per
  shift.
- **A floor** per uplift kind.
- **Which hours count as night.**
- **What is auto-assigned to a newly created shift** regarding combined uplifts.

<!-- level:elements -->

## Stage 2 — The fields

Card anchor: `surcharge-mode-settings-container`.

### How a rate is read (de: "Zuschlagsmodi")

Set separately for night, holiday and the three weekend slots:

- **Multiple** (de: "Multiplikator") — the figure is applied to the hours in that stretch.
- **Fixed per hour** (de: "Fixbetrag pro Stunde") — arithmetically the same as a multiple; only the
  meaning of the figure differs.
- **Lump sum per shift** (de: "Fixbetrag pro Schicht") — this one really is different: the figure is
  granted **once**, regardless of the number of hours, and remains zero when the uplift does not
  apply at all.

### Floors (de: "Mindestbeträge")

A floor per hour can be put under each uplift kind. It raises a computed uplift to that level.

**It needs all three of these, or it silently does nothing:**

1. the corresponding kind must be set to **multiple** — with the two fixed modes the floor is
   ignored entirely,
2. the field must be filled,
3. the rate itself must not be zero.

The third condition catches people out with the **third weekend slot**, whose rate is zero by
default. A floor entered there stays without effect until a rate is set.

### Night window (de: "Nachtzeitfenster")

The start and end of night, by default 23:00 to 06:00. A stretch of work crossing the boundary is
apportioned to the minute, and a window running past midnight is handled correctly.

### The three weekend slots

The slots are not fixed weekdays. They stand for the first, second and third configured weekend
day, in the order of the week. With a normal Saturday-and-Sunday setup the first slot is Saturday,
the second Sunday, and **the third is unused and never matches**.

<!-- level:effects -->

## Stage 3 — When several uplifts meet

This is the field most often misread — including by the hint printed beside it.

**The choice on this card does not change any existing calculation.** It decides only what is
attached to a **newly created** shift. Changing it leaves every existing shift, and every figure
already calculated, exactly as it was. To change how an existing shift combines uplifts, that
shift's own calculation has to be changed.

**How combining actually works** depends on the calculation attached to the shift, and there are two
kinds:

- **Highest wins** — within a given stretch of time, only the largest applicable uplift is paid.
  **This works per stretch, not per shift**: a shift is first split into its night part and its
  non-night part, each part is resolved on its own, and the two results are **added**. One shift can
  therefore legitimately produce two uplift entries. Where two rates are equal, the earlier check
  wins, in the order night, holiday, first weekend slot, second, third.
- **Additive** — every applicable uplift is paid alongside the others. Night on a public holiday
  falling on a Sunday pays all three.

A second, separate question is what happens when an uplift meets extra hours. That too follows the
shift's own calculation, not this card.

## What belongs here and what does not

Rates and the night window can be set per person through their working conditions or the rule
preset attached to them. **The reading mode, the floors and the combining behaviour cannot** — they
exist once for the whole installation.

## Related skills

- `get_surcharge_mode_settings`, `update_surcharge_mode_settings`

## Trigger phrases

- "Addieren sich Nacht- und Sonntagszuschlag?"
- "Ich habe den Modus umgestellt, es ändert sich nichts."
- "Was bedeutet die Zahl im Zuschlagsfeld?"
- "Der Mindestbetrag wirkt nicht."
- "Ab wann gilt Nacht?"
