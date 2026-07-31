---
name: explain_company_rules
description: |
  Explains how a company rule stated in plain words becomes a setting: Klacksy opens a draft,
  collects the required values one by one, shows a before-and-after preview, and only writes
  anything after an explicit go-ahead. Applied rules are listed and can be taken back later. Covers
  the three kinds of rule, why nothing is stored before the preview, and the limits of reverting.
  Use this when the user wants to state a company rule in their own words, asks what a rule changed,
  or wants one undone.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - firmenregel
  - company rule
  - betriebsregel
  - regel festlegen
  - vorschau
synonyms:
  de: [firmenregel, betriebsregel, hausregel, regel festlegen, regel anlegen, vorschau vor anwenden, regel zurücknehmen, welche regeln gelten]
  en: [company rule, house rule, set a rule, preview before applying, revert a rule, which rules apply]
  fr: [règle interne, règle d entreprise, définir une règle, aperçu avant application, annuler une règle]
  it: [regola aziendale, regola interna, definire una regola, anteprima prima di applicare, annullare una regola]
---

# Company rules — from a sentence to a setting

## Core idea (one sentence)

An administrator states a rule in their own words, and Klacksy turns it into a concrete setting —
but only after showing exactly what would change and getting an explicit go-ahead.

## The four steps

1. **Start.** Klacksy opens a draft, keeps the original wording, and returns a checklist of the
   values it still needs — which are mandatory and which optional.
2. **Collect.** Values are filled in one by one. Each is checked on its own: an invalid value is
   reported and **not** stored, valid ones are kept. Klacksy reports what is still missing.
3. **Preview.** Before anything is written, the full effect is shown — for changed surcharges the
   old-to-new comparison of every affected setting, for a limit rule the scope it will apply to, for
   a custom calculation the script together with its validation result.
4. **Apply or discard.** Applying writes the change and records it in a register. Discarding throws
   the draft away.

**Nothing is stored before step four.** A draft lives only for the conversation; abandoning it
changes nothing. Only one draft is in progress at a time — starting a new rule replaces the previous
draft.

## The three kinds of rule

- **Surcharge settings** — the percentages for night, holiday and weekend work.
- **A limit rule** — a constraint on planning, such as a maximum number of consecutive night shifts.
- **A custom calculation** — a formula for cases the standard settings do not cover.

## Taking a rule back

Applied rules are listed with their name, kind, target and the date they were applied. A rule can be
reverted by name: the overwritten surcharge settings are restored from the snapshot taken at the
time, or the limit rule or calculation it created is removed, and the register entry disappears.

Worth knowing: reverting restores **the settings the rule overwrote**, not the plans that were made
in the meantime. A plan built while the rule was in force keeps the values it was built with.

## Why the detour

Stating a rule in plain words is quick but ambiguous; a setting has to be exact. The preview is
where the two meet: the administrator sees Klacksy's interpretation in concrete numbers before it
becomes reality, instead of discovering a misunderstanding weeks later in a payroll run.

## Related skills

- `start_company_rule` — open a draft from the wording
- `set_company_rule_parameters` — fill in the values
- `preview_company_rule` — see the effect before it happens
- `apply_company_rule` / `cancel_company_rule` — write it, or throw the draft away
- `list_company_rules` / `revert_company_rule` — what applies, and taking it back

## Trigger phrases

- "New company rule: at most three night shifts in a row."
- "What would that change exactly?"
- "Which rules are currently in force?"
- "Take back the rule from last week."
- "Der Nachtzuschlag soll neu 25 Prozent sein."
