---
name: explain_period_closing_model
description: |
  Explains what closing a payroll period actually does: it locks the days in that period, writes an
  audit entry and can hand the figures to payroll. Covers the pre-flight check to run before closing,
  the difference between approving a day and closing a period, how an hours balance is made up of
  actual against target hours, and that reopening is possible but administrator-only. Use this when
  the user asks why a day can no longer be edited, what to check before closing, or how the balance
  in the schedule row is calculated.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - periode abschliessen
  - period closing
  - gesperrt
  - saldo
  - überstunden
  - wieder öffnen
synonyms:
  de: [periode abschliessen, periodenabschluss, tag gesperrt, kann nicht mehr bearbeiten, saldo, überstunden, minusstunden, wieder öffnen, prüfliste vor abschluss, lohnexport]
  en: [close period, period closing, day locked, cannot edit any more, balance, overtime, undertime, reopen period, pre-flight check, payroll export]
  fr: [clôture de période, jour verrouillé, solde, heures supplémentaires, rouvrir la période, export paie]
  it: [chiusura periodo, giorno bloccato, saldo, ore straordinarie, riaprire il periodo, esportazione paghe]
---

# Closing a period — what actually happens

## Core idea (one sentence)

Closing a period freezes the days inside it so the figures handed to payroll cannot change
afterwards.

## What closing does

Four things happen at once:

1. The work and absences in the period are **locked** — they can no longer be edited.
2. **Day locks** are written, and these are the authoritative record of what was frozen.
3. An **audit entry** is written, so it stays traceable who closed which period when.
4. If the closing is scoped to a group, the **hand-off to payroll** is triggered.

This is why a day suddenly refuses to be edited: it belongs to a closed period. That is not a fault,
it is the point.

## Check before you close

There is a pre-flight check that lists the validation findings of the period — errors, warnings and
notes per day and per person. It is meant to be run **before** closing, because afterwards
correcting anything means reopening the whole period.

Typical findings are days without an assignment, hours that contradict the contract, or absences
that were never materialised from a wish.

## Approving is not closing

Approving a day or a group in the schedule and closing a period are different acts by different
people. Approval is the planner's confirmation that a day is correct, and it can be withdrawn again.
Closing is the final freeze for payroll and is **administrator-only** — including reopening.

So a period can perfectly well contain approved days and still be open.

## How the balance is made up

The balance shown in a person's schedule row is the difference between two figures:

- **actual hours** — what is scheduled, including surcharges,
- **target hours** — what the contract requires (see `explain_contracts`).

The same comparison can be swept across a whole group, sorted from the most under-target to the most
overtime — which is how the question "who has too many hours" gets answered without going through
people one by one.

## Expenses

Expenses belong to the period as well: they are recorded per person and go into the same hand-off.
Worth entering before closing, for the same reason as everything else.

## Related skills

- `list_period_issues` — the pre-flight check, before closing
- `close_period` / `reopen_period` — freeze and unfreeze, administrator-only
- `list_open_periods` / `get_period_status` / `list_period_audit_log` — what is open, and what happened
- `get_period_hours` / `get_group_hours_balance` — actual against target, per person or group
- `add_expense` / `update_expense` / `delete_expense` / `list_expenses` — expenses
- `generate_period_summary` — a summary of the period

## Trigger phrases

- "Why can't I edit this day any more?"
- "What should I check before closing?"
- "Who has overtime this month?"
- "Reopen the period."
- "Was ist der Unterschied zwischen genehmigen und abschliessen?"
