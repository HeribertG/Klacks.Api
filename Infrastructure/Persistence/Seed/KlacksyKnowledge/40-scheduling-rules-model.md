---
name: explain_scheduling_rules_model
description: |
  Explains the limits a roster has to respect: the longest working day, the ceiling on hours per
  week, how many days in a row somebody may work, the minimum hours off between two working days
  and the minimum rest days between two blocks of work. Covers where a limit that is left empty
  takes its value from, what an entered zero actually does, and whether exceeding a limit only
  warns the planner or refuses the assignment. Use this when the user asks why the planner objects
  to a shift, which limit applies to whom, or how strictly a limit is enforced.
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
    - ruhezeit
    - höchstarbeitszeit
    - arbeitszeitgrenze
    - aufeinanderfolgende tage
    - planungsregel
    - grenzwert
    - wochenstunden
  en:
    - rest period
    - working time limit
    - consecutive days
    - scheduling rule
    - weekly hours cap
synonyms:
  de: [planungsregel, ruhezeit, ruhetage, höchstarbeitszeit, arbeitszeitgrenze, maximale wochenstunden, maximale tagesstunden, aufeinanderfolgende arbeitstage, pause zwischen arbeitstagen, grenzwert überschritten, warum meckert die planung]
  en: [scheduling rule, rest period, rest days, maximum daily hours, maximum weekly hours, consecutive working days, break between working days, limit exceeded, why does planning complain]
  fr: [règle de planification, temps de repos, jours de repos, heures maximales par jour, heures maximales par semaine, jours de travail consécutifs, limite dépassée]
  it: [regola di pianificazione, tempo di riposo, giorni di riposo, ore massime giornaliere, ore massime settimanali, giorni lavorativi consecutivi, limite superato]
---

# Scheduling rules — the limits a roster has to respect

<!-- level:short -->

## Stage 1 — What this is for

A scheduling rule is a named set of upper and lower limits for working time. It answers one
question: *how far may a roster go before somebody has to look at it?*

The limits live in two places. A named rule
(de: "Individuelle Planungsregeln", en: "Individual Scheduling Rules",
fr: "Règles de planification individuelles", it: "Regole di pianificazione individuali") applies to
the people whose working conditions point at it. Alongside it stands one company-wide set of
defaults (de: "Planungsregeln (Standardwerte)", en: "Scheduling Rules (Defaults)",
fr: "Règles de planification (valeurs par défaut)", it: "Regole di pianificazione (valori predefiniti)")
that catches everything a named rule leaves open.

A rule never has a validity date of its own and never competes with another rule: each set of
working conditions points at exactly one rule, or at none.

<!-- level:elements -->

## Stage 2 — The limits in detail

Anchor for this card: `scheduling-rules-table-header`. The edit dialog is
`scheduling-rule-modal-template`.

**Time limits**

- **Longest working day** (de: "Max. Tagesstunden", en: "Max. Daily Hours",
  fr: "Max. heures journalières", it: "Max. ore giornaliere") — hours on one calendar day.
  Anchor `scheduling-rule-modal-max-daily-hours-item`.
- **Hours per week** (de: "Max. Wochenstunden", en: "Max. Weekly Hours",
  fr: "Max. heures hebdomadaires", it: "Max. ore settimanali") — counted per ISO week.
  Anchor `scheduling-rule-modal-max-weekly-hours-item`.
- **Days in a row** (de: "Max. aufeinanderfolgende Arbeitstage", en: "Max. Consecutive Work Days",
  fr: "Max. jours de travail consécutifs", it: "Max. giorni lavorativi consecutivi").
  Anchor `scheduling-rule-modal-max-consecutive-days-item`.
- **Hours off between two working days** (de: "Min. Freistunden zwischen zwei Arbeitstagen",
  en: "Min. Free Hours Between Two Work Days", fr: "Min. heures libres entre deux jours de travail",
  it: "Min. ore libere tra due giorni lavorativi").
  Anchor `scheduling-rule-modal-min-pause-hours-item`.
- **Rest days between two blocks of work** (de: "Min. Ruhetage zwischen zwei Arbeitsblöcken",
  en: "Min. Rest Days Between Two Work Blocks", fr: "Min. jours de repos entre deux blocs de travail",
  it: "Min. giorni di riposo tra due blocchi lavorativi") — may be a fraction of a day.
  Anchor `scheduling-rule-modal-min-rest-days-item`.
- **Working days** (de: "Max. Arbeitstage", en: "Max. Work Days", fr: "Max. jours de travail",
  it: "Max. giorni lavorativi") and **optimal gap between shifts**
  (de: "Max. optimale Lücke zwischen Schichten (Std.)", en: "Max. Optimal Gap Between Shifts (hrs)",
  fr: "Max. écart optimal entre quarts (h)", it: "Max. intervallo ottimale tra turni (ore)").
  These two guide the planning proposal; they are not part of the checks that can refuse an
  assignment.

**Hours a person is owed** — daily working hours, overtime threshold, guaranteed, maximum, minimum
and full-time hours, and vacation days per year. A rule may carry its own figures here; if it does
not, the figures come from elsewhere (see Stage 3).

**Rates** — night, public holiday, Saturday and Sunday. A rule sets *how high* a rate is. *How*
several rates combine is decided company-wide and cannot be changed per rule.

**Working weekdays and shift work** — which days this rule foresees work on, and a marker for
early/late/night duty.

**The only checks when saving:** the name must not be empty (de: "Name ist erforderlich."),
and no value may be negative (de: "Werte müssen 0 oder grösser sein."). Nothing checks a limit
against another limit — a minimum above the maximum, or a daily figure that cannot add up to the
weekly one, is accepted.

<!-- level:effects -->

## Stage 3 — Where an empty limit gets its value

An empty field does not mean "no limit". It means "take it from somewhere else", and the chain
differs by field group. This is the part people get wrong most often.

| Field group | Chain |
|---|---|
| Maximum, minimum, full-time hours, guaranteed hours, all rates, the night window | rule → working conditions → company defaults |
| Daily working hours, overtime threshold, and every time limit from Stage 2 | rule → company defaults (working conditions are skipped) |
| Working weekdays, shift-work marker | rule → working conditions. With active working conditions the company setting is unreachable. |
| How rates combine, and any minimum amount per hour | company only — a rule can never override these |

Two exceptions:

- When somebody is paid against a table of monthly target hours and a figure exists for that month,
  the company figure wins **over** the rule, not the other way round.
- A dated revision of the rates replaces the rule's rate fields as a block. A value left empty in
  the revision does not fall back to the rule.

**A zero is not the same as an empty field.** For the five limits that can refuse an assignment —
longest working day, hours per week, days in a row, hours off between working days, rest days —
a value of zero or less is treated as "not configured" and a built-in figure applies instead:
11 hours off, 10 hours a day, 6 days in a row, 50 hours a week, 2 rest days. These exist so an
empty database cannot switch every check off.

## What happens when a limit is exceeded

By default the planner is **warned** and may proceed. Per limit this can be switched to refusing
the assignment, and there is one company-wide setting for everything that has no choice of its own.

Refusal does not look the same everywhere: when planning, the affected rows are dropped from the
proposal and the rest goes through; when accepting a scenario, the whole operation is rejected;
when searching for a replacement, the person is left out of the candidate list.

Some objections cannot be waved through at all — a double booking or a missing required
qualification is refused regardless of role and regardless of any setting. Only an objection that
comes from a configurable limit can be overridden, and only by somebody with the right to do so.

## Related skills

- `list_scheduling_rules`, `create_scheduling_rule`, `update_scheduling_rule`, `delete_scheduling_rule`
- `get_scheduling_defaults`, `update_scheduling_defaults`

## Trigger phrases

- "Why does the planner say this shift is not allowed?"
- "How many days in a row may somebody work?"
- "Wie viele Stunden Pause müssen zwischen zwei Diensten liegen?"
- "Ich habe das Feld leer gelassen — was gilt dann?"
- "Is that a warning or does it really stop me?"
