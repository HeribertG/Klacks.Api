---
name: explain_period_closing_model
description: |
  Explains what closing a payroll period actually does: it locks the days in that period, writes an
  audit entry and freezes the figures for payroll. Covers the pre-flight check to run before closing,
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

Closing a period freezes the days inside it so that the figures handed to payroll cannot change afterwards.

<!-- level:short -->

## What closing a period means

A period is the stretch of time you settle together, for example a month. Closing it is the last step once the hours are final: from then on nobody can change the work and the absences of those days any more. Only an administrator can close a period, and Klacks records who closed what and when. A day that suddenly refuses to be edited usually belongs to a closed period; that is not a fault, it is the point. Example: once April is closed, a forgotten sick day in April cannot be added until an administrator reopens the period.

<!-- level:elements -->

## Closing and reopening, step by step

The page is for administrators only (de: "Periodenabschluss", en: "Period Closing", fr: "Clôture de période", it: "Chiusura periodo").

1. **Pick the period.** In the card (de: "Perioden", en: "Periods") choose it from the dropdown (de: "Periode", en: "Period"). Periods of a group carry the group name.
2. **Check it first.** Open the card (de: "Probleme dieser Periode", en: "Issues in this period", fr: "Problèmes de cette période", it: "Problemi di questo periodo"). It lists errors, warnings and notes per day and per person, typically days without an assignment, hours that contradict the contract, or absences that were never booked from a wish. Correct what needs correcting now, because afterwards every correction means reopening.
3. **Close.** Click (de: "Alle versiegeln", en: "Seal all") and confirm. If errors are still open, a second question follows (de: "Trotzdem abschliessen", en: "Close anyway", fr: "Clôturer quand même", it: "Chiudi comunque"). Warnings never stop the closing, errors do until you confirm.
4. **Reopen.** Click (de: "Alle entsiegeln", en: "Unseal all") or untick a single day. A reason is required in the box that opens (de: "Begründung", en: "Reason"); the button is (de: "Periode wieder öffnen", en: "Reopen period", fr: "Rouvrir la période", it: "Riapri periodo").
5. **Look up who did what.** The card (de: "Protokoll", en: "Audit Log", fr: "Journal d'audit", it: "Registro di controllo") lists every closing and reopening with the person, the reason and the number of entries.

<!-- level:effects -->

## What closing does and how it connects to the rest

### What happens at once

1. The work entries and absences of the period are set to the highest lock stage (closed). Only the real plan is touched, never scenario drafts.
2. A day lock is written for every day of the period. The day locks are the authoritative record of what was frozen.
3. An audit entry is written in the same transaction: who, when, which period, which group, the reason and how many entries were affected.

With a group chosen, the closing covers the entries of that group and the day locks belong to that group; without a group it is global.

### What a locked day refuses

On a locked day the server refuses to create, change or delete work entries, work corrections, absences, expenses, notes and planning commands (message de: "Tag ist gesperrt und kann nicht geändert werden", en: "Day is sealed and cannot be modified"). A day lock without a group blocks every person; a group's day lock blocks the persons who belong to that group on that day. Scenario drafts are not checked. Planned absence wishes are not checked either.

### Approving is not closing

An entry has four lock stages: open, confirmed, approved, closed. Confirming is the first step and needs no special role. Approving a day for a group needs a supervisor or an administrator; it also writes a day lock for that group, so the day is already locked for its members, and the approving group can withdraw it again. Closing covers a whole period, needs an administrator, and so does reopening; a reason is mandatory.

### Payroll

Closing starts no payroll export. An administrator exports the payroll figures afterwards, per employee, from the same page. That export is blocked while an entry of the range is not closed or a day is not locked by a closing.

### How the balance is made up

The row of a person in the schedule shows three figures, each with a tooltip: de: "Garantierte Stunden", en: "Guaranteed Hours"; de: "Geleistete Stunden", en: "Worked Hours" (including surcharges); de: "Zuschlag", en: "Surcharges". It shows no balance itself. The balance that Klacksy reports is the worked hours without surcharges minus the guaranteed hours, so a negative number means under target. The guaranteed hours come from the contract. The same comparison can be swept across a whole group, sorted from the most under target to the most overtime.

### Expenses

Expenses are recorded per person and day and go into the same payroll hand-off. A locked day refuses expense changes, so enter them before closing.

### Related skills

- `list_period_issues` — the pre-flight check, before closing
- `close_period` / `reopen_period` — freeze and unfreeze, administrator-only
- `list_open_periods` / `get_period_status` / `list_period_audit_log` — what is open, and what happened
- `get_period_hours` / `get_group_hours_balance` — actual against target, per person or group
- `approve_day` / `revoke_day_approval` — approval of a day for a group
- `add_expense` / `update_expense` / `delete_expense` / `list_expenses` — expenses
- `generate_period_summary` — a summary of the period

### Trigger phrases

- "Why can't I edit this day any more?"
- "What should I check before closing?"
- "Who has overtime this month?"
- "Reopen the period."
- "Was ist der Unterschied zwischen genehmigen und abschliessen?"

<!-- level:pitfalls -->

## Consequences and edge cases

**There is no general step-back function in Klacks.** After a closing the only way back is the reopening by an administrator, with a reason. Reopening gives every entry the lock stage it had before the closing: confirmed stays confirmed, approved stays approved, open stays open. Only entries that were closed before this was recorded have no stored previous stage; they come back open, and an earlier confirmation or approval of theirs is lost. Reopening also lifts the day locks and writes its own audit entry.

**A reopening does not take back what left the system.** The page warns when an export already exists for the period. Earlier exports stay in the export history, and after correcting and closing again the next payroll export contains only the persons whose figures changed, marked as a supplement.

**A reopening by group lifts only that group's locks.** An absence of a person who belongs to several groups stays closed when it was closed by the first group and another group reopens; a day approval of another group can keep a day locked as well.

**Errors gate the closing, warnings do not.** If errors appear between the refusal and your confirmation, the confirmation is refused again. Closing over a known error is possible, but it is a decision: the closing makes those days unwritable.

**Klacksy closes on its own only when an administrator has switched that on.** It is off by default. It needs the rule for the automatic period close set to carry out, full autonomy of the installation and of every administrator, a stored closing time, and a period with no open errors. A period that somebody reopened is never closed automatically again. Every automatic closing appears in the audit log.

**The guaranteed hours are a planning target, not a payroll figure.** If the membership of a person starts or ends inside the period, the target is scaled by the member days. The worked hours of a person are stored per period and updated when entries are written; the recalculation in the schedule toolbar (de: "Stunden berechnen und zusammenzählen", en: "Calculate and sum hours") skips entries that are confirmed, approved or closed, so a change of a macro does not reach locked days until they are reopened.

**Scenario drafts are outside the closing.** A closing neither locks nor changes the entries of a scenario, and a locked day does not stop scenario entries.
