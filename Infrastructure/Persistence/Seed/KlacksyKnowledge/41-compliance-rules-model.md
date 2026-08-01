---
name: explain_compliance_rules_model
description: |
  Explains four families of working-time safeguards that go beyond a plain daily or weekly ceiling:
  counting recurring events per person (night shifts a year, worked days a week, shifts above a
  given length), capping hours across a month, quarter or a rolling window of weeks by average,
  banning a slice of the day during a season for a tagged group, and owing somebody replacement
  rest after a short break. Covers where each family's severity is decided and who may push past a
  refusal. Use this when the user asks about a yearly night-shift ceiling, an averaging window, a
  seasonal ban such as a midday heat stop, or rest that has to be made up later.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  mul:
    - compliance
  de:
    - zählerregel
    - nachtschichten pro jahr
    - obergrenze
    - gleitfenster
    - sperrzeit
    - ersatzruhe
    - hitzeverbot
  en:
    - counter rule
    - period cap
    - rolling window
    - restricted time window
    - compensatory rest
synonyms:
  de: [zählerregel, zähler-regel, nachtschichten pro jahr, perioden-obergrenze, gleitfenster, gleitender durchschnitt, gesperrtes zeitfenster, sperrzeit, hitzeverbot, ersatzruhe, ruhe nachholen, wie viele nachtschichten sind erlaubt]
  en: [counter rule, period cap, rolling window, rolling average, restricted time window, seasonal ban, compensatory rest, make up rest, how many night shifts are allowed]
  fr: [règle de comptage, plafond de période, fenêtre glissante, moyenne glissante, fenêtre horaire restreinte, repos compensateur, combien de postes de nuit]
  it: [regola di conteggio, limite di periodo, finestra mobile, media mobile, finestra oraria vietata, riposo compensativo, quanti turni notturni]
---

# Working-time safeguards — counters, caps, banned windows and replacement rest

<!-- level:short -->

## Stage 1 — What this is for

Four separate safeguards sit alongside the plain limits on a working day. Each answers a question
that a daily or weekly ceiling cannot:

- **Counter rules** (de: "Zähler-Regeln", en: "Counter rules", fr: "Règles de comptage",
  it: "Regole di conteggio") — *how often* may something recur? For example at most 25 night shifts
  a year, or at most 6 worked days in a week.
- **Period caps** (de: "Perioden-Obergrenzen", en: "Period caps", fr: "Plafonds de période",
  it: "Limiti di periodo") — how many hours across a month, quarter, year, or on average across a
  window of weeks. The German 24-week / 48-hour average is such a cap.
- **Restricted time windows** (de: "Gesperrte Zeitfenster", en: "Restricted time windows",
  fr: "Fenêtres horaires restreintes", it: "Finestre orarie vietate") — a slice of the day that is
  off limits during a season, optionally only for a tagged group. The midday heat stop used in the
  Emirates from mid-June to mid-September is one.
- **Compensatory rest** (de: "Ersatzruhe", en: "Compensatory rest", fr: "Repos compensateur",
  it: "Riposo compensativo") — when a break falls short, the missing rest has to be made up within
  a deadline.

The first three are lists of rules you create. The fourth is a single company-wide switch.

**These rules do not cascade.** A rule is not attached to a company, a group, a set of working
conditions or a person, and one rule never overrides another. A counter rule and a period cap may
optionally be limited to one industry preset — that is a flat filter, not a hierarchy. A restricted
window is limited by a group tag instead: free text matched against the group name, empty meaning
every shift.

<!-- level:elements -->

## Stage 2 — The fields

Card anchors: `counter-rules`, `period-cap-rules`, `restricted-time-window-rules`,
`compensatory-rest`, `compliance-enforcement`.

### Counter rules

- **Event type** (de: "Ereignistyp") — night shift, worked day per week, or shift exceeding a given
  number of hours.
- **Period** (de: "Zeitraum") — week, month or year.
- **Threshold** (de: "Schwellwert") — how many occurrences are still acceptable. Must be above zero.
- **Hours threshold** (de: "Stunden-Schwellwert") — appears **only** for the third event type, and
  is then mandatory.
- **Enforcement** (de: "Durchsetzung") — this family alone may set severity per row; left empty it
  takes the company setting.

A subtlety worth knowing: *night shift* counts against a night window that is **not** on this card.
It comes from the person's effective working conditions, falling back to 23:00–06:00. The same
counter rule can therefore mean different things for two people.

### Period caps

Two mutually exclusive modes — one or the other, never both, and never neither:

- **Fixed cap**: a period (month, quarter, year, or a custom number of weeks from 1 to 104) plus a
  ceiling in hours (de: "Obergrenze (Std.)"). Custom weeks require the week count and forbid it
  otherwise.
- **Rolling window**: a number of weeks (de: "Gleitfenster (Wochen)") plus the highest acceptable
  weekly average (de: "Max. Ø Wochenstunden"). Both are needed together.

**Scope** (de: "Geltungsbereich") does *not* mean an organisational level — it selects which hours
are counted: all hours, or overtime only.

**Warn at (%)** is reserved. It validates and stores, but nothing evaluates it today; the hint on
the field says so.

The two modes are also severity-wise separate: a fixed cap follows the *period cap* setting, a
rolling window follows the *rolling average* setting. Tightening only the first leaves the window
untouched.

### Restricted time windows

Season from month/day to month/day, a daily start and end, and an optional group tag
(de: "Gilt für Gruppen-Tag"; empty = every shift).

An end before the start is deliberate and means the window crosses midnight (22:00–06:00); likewise
a season end before its start crosses the turn of the year. Month and day are checked as
independent ranges, so an impossible date such as 31 February passes validation.

### Compensatory rest

Enabled, plus a deadline in days. The threshold that triggers an obligation is **not** on this card
— it is derived from the minimum break in the scheduling defaults and is edited there.

**Auto-planning of replacement rest is not implemented.** The checkbox exists, but nothing acts on
it, and the setup import rejects a configuration that switches it on. Making the rest up is a
manual step.

<!-- level:effects -->

## Stage 3 — How severity is decided

Severity is configured centrally (de: "Compliance-Durchsetzung", en: "Compliance enforcement"),
never on the rule itself — except for counter rules, which may override per row.

The chain, most specific first:

1. the counter rule's own setting, if filled
2. the per-rule setting for that safeguard
3. the company-wide default
4. failing everything: **warn**

Eleven safeguards can be set individually, each as "use default", "warn" (de: "Warnen") or "block"
(de: "Blockieren"): longest working day, hours per week, minimum rest hours, minimum rest days,
days in a row, period cap, rolling average, rest-day rotation, counter rule, compensatory rest, and
restricted time window. Note that severity is set here while the *values* for the first five come
from the scheduling defaults.

**What blocking looks like depends on the path:**

- While planning, the affected rows are dropped and the rest of the proposal goes through — this is
  a normal result, not an error.
- Accepting a scenario is rejected as a whole.
- Searching for a replacement leaves the person out of the candidate list.

**Two limits to this picture, both worth stating plainly:**

- A **restricted time window is always an absolute veto inside the planning engine**, whatever the
  severity says. Setting it to "warn" does not get a shift placed in a banned window; it only
  softens the entry the validator writes afterwards.
- **Blocking compensatory rest does not stop a save.** That safeguard is not part of the check that
  runs before writing; it surfaces at period closing and when accepting a scenario.

**Overriding a refusal** requires all three: the right role, the company setting that permits it,
and an explicit request to override. Every override is recorded. Objections that do not come from a
configurable safeguard — a double booking, a missing required qualification — can never be waved
through, by anyone.

## A note on deleting rules

Rules that arrived through a regional setup come back when that setup is imported again. Deleting
such a row is not permanent unless it is also removed from the regional file. Rules created by hand
are never touched by an import.

## Related skills

- `list_counter_rules`, `create_counter_rule`, `update_counter_rule`, `delete_counter_rule`
- `list_period_cap_rules`, `create_period_cap_rule`, `update_period_cap_rule`, `delete_period_cap_rule`
- `list_restricted_time_window_rules`, `create_restricted_time_window_rule`, `update_restricted_time_window_rule`, `delete_restricted_time_window_rule`
- `get_compliance_enforcement_settings`, `update_compliance_enforcement_settings`
- `get_compensatory_rest_settings`, `update_compensatory_rest_settings`

## Trigger phrases

- "How many night shifts per year are allowed?"
- "Wir brauchen einen Durchschnitt über 24 Wochen — geht das?"
- "Can I ban outdoor work at midday in summer?"
- "Was passiert, wenn jemand zu wenig Ruhezeit hatte?"
- "Warnt das nur oder verhindert es die Zuteilung wirklich?"
