---
name: explain_target_hours
description: |
  Explains how the target hours of an employee for a month are resolved, from the first to the last
  applicable source: no contract (monthly row, otherwise settings, at 100 %), a fixed contract (contract or
  scheduling-rule hours), Weg 1 (interval "Monthly (target hours table)" plus percent) and Weg 2 (guaranteed
  hours left empty plus percent), how the view interval decides which contract is seen, the 150 % plausibility
  guard and the proration to planning periods. Includes a decision guide which way fits which situation and
  where every value is set. Use when somebody asks why an employee has a certain target or how to set it up.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - sollzeit
    - sollstunden berechnet
    - wie werden die sollstunden
    - woher kommen die sollstunden
    - sollstunden eines mitarbeiters
    - pensum und sollstunden
  en:
    - target time
    - target hours calculated
    - how are the target hours
    - where do the target hours come from
    - target hours of an employee
    - workload and target hours
synonyms:
  de: [wie kommen die sollstunden zustande, welcher weg für die sollstunden, sollstunden ohne vertrag, sollstunden mit pensum]
  en: [how do the target hours come about, which way for the target hours, target hours without a contract, target hours with a workload percent]
  fr: [comment sont calculées les heures cibles, heures cibles sans contrat, heures cibles avec taux d'occupation]
  it: [come si calcolano le ore obiettivo, ore obiettivo senza contratto, ore obiettivo con percentuale di impiego]
---

# Target hours — how the monthly target of an employee is resolved and which way to choose

## Core idea (one sentence)

The target hours of a month come from **one of four ways** — no contract, fixed contract hours, Weg 1 (table interval plus percent) or Weg 2 (empty guaranteed hours plus percent) — and the monthly value of the table is always the **target time of a 100 % position** for that month, which a contract's percent scales.

<!-- level:short -->

## Stage 1 — What it is and what it is for

Every employee has a target for each pay period (usually the calendar month); the schedule shows it in the row header (de: "Garantierte Stunden", en: "Guaranteed Hours") and compares it with the worked and absence hours. Where the target comes from depends on the contract: a fixed number of hours, or the company's month value scaled by a percent, or - without a contract - the company value at 100 %. The monthly value (de: "Monatliche Sollstunden", en: "Monthly target hours") is the target time of a 100 % position for this month; it applies to employees without a contract (at 100 %) and to contracts on the interval (de: "Monatlich (Sollstunden-Tabelle)", en: "Monthly (target hours table)") or without guaranteed hours - there as monthly value × percent. Contracts with fixed guaranteed hours are not affected by it.

<!-- level:elements -->

## Stage 2 — The resolution, first to last applicable source

1. **No contract on the first day of the period:** the monthly value of the table for that month, otherwise the company guaranteed hours (de: "Garantierte Stunden", en: "Guaranteed Hours" of the work settings, section de: "Arbeitszeit & Planung", en: "Work & Scheduling") - always at 100 %. Any other workload share needs a contract.
2. **Fixed contract** (guaranteed hours filled in): the scheduling rule's guaranteed hours if the rule sets them, otherwise the contract's (de: "Garantierte Stunden", en: "Guaranteed Hours" of the contract). The table is ignored. The workload share for absence days is guaranteed hours ÷ full time (de: "Vollzeit", en: "Full Time"); a full time of 0 falls back to the company full time.
3. **Weg 1 — interval (de: "Monatlich (Sollstunden-Tabelle)", en: "Monthly (target hours table)") plus percent (de: "Pensum (%)", en: "Workload (%)"):** monthly value × percent in every month that has a row; in a month without a row the contract's own guaranteed hours, otherwise the company guaranteed hours × percent.
4. **Weg 2 — guaranteed hours left empty plus percent, on any interval:** (monthly value, otherwise company guaranteed hours) × percent. The scheduling rule's guaranteed hours are ignored on this way.
5. **Proration:** the period target is scaled by the membership window of the person (member days ÷ days of the period, rounded to two decimals). The planning wizards additionally prorate the target to their planning range per day of the pay period.
6. **Plausibility guard:** a derived workload share above 150 %, or no positive full time, falls back to 100 % for absence days and is logged - a sign of broken master data (for example monthly hours against a weekly full time).

### Which contract is seen

The period target reads the contract regardless of the view. Booking an absence, however, uses the payment interval of the schedule view (the group's, otherwise the company interval): (de: "Monatlich", en: "Monthly") and (de: "Monatlich (Sollstunden-Tabelle)", en: "Monthly (target hours table)") count as one; weekly, two-weekly and individual contracts are not seen by a monthly view and the employee counts as having no contract for that booking. Two contracts starting on the same day: the one created last applies.

### Decision guide — which way for which situation

| Situation | Way | Set it in |
|---|---|---|
| Same workload all year, hours known | fixed contract | contract: guaranteed hours, full time |
| Company month values vary (table maintained), employee at a share of it | Weg 1 (table interval + percent) or Weg 2 (empty hours + percent) | contract: payment interval or empty guaranteed hours, plus percent |
| Employee at 100 % and the company values are right for them | no contract is possible, but a contract on Weg 2 with an empty percent is clearer | contracts card of the person |
| One month differs for everybody | monthly table row for that month | table (de: "Monatliche Sollstunden", en: "Monthly target hours") |

Weg 1 and Weg 2 give the same result in months with a row; they differ in months without a row (Weg 1: contract hours; Weg 2: company hours × percent) and in which interval the schedule groups work in.

### Where each value lives

- Contract fields: settings page (de: "Verträge", en: "Contracts") - payment interval (de: "Zahlungsintervall", en: "Payment Interval"), guaranteed hours, full time, percent. A contract is a template shared by everybody assigned to it.
- Assignment to a person: contracts card of the person (de: "Verträge", en: "Contracts"), button (de: "Vertrag hinzufügen", en: "Add Contract"); only the active assignment of the period start counts.
- Company values: work settings - guaranteed hours, full time (de: "Vollzeit (Stunden)", en: "Full Time (hours)"), payment interval.
- Month values: table (de: "Monatliche Sollstunden", en: "Monthly target hours"). Fill every month of a year, or the basis switches between month value and settings or contract value.
- Scheduling rule (de: "Planungsregeln", en: "Scheduling Rules"): overrides guaranteed hours and full time on a fixed contract only.

`get_target_hours_calculation` shows the real resolution of an employee for a month with every source; `evaluate_target_hours_setup` says which way fits and what to change; `explain_absence_hours` covers what a vacation day of that employee credits. Every change it leads to - a contract (`update_contract`, `create_contract_from_template`), a month row (`create_monthly_target_hours`, `update_monthly_target_hours`) or an assignment (`assign_contract_by_name`) - is a separate step the system confirms once before writing, so do not ask a separate "shall I?" first.

<!-- level:effects -->

## Stage 3 — What the choices cause

- **A percent on a fixed contract has no effect** on the target; it scales only a month row or the inherited company value.
- **Fixed hours on the table interval** apply only in months without a row - the row wins where it exists.
- **A partly filled table switches the basis** from month to month for everybody without a contract and for Weg 1 and Weg 2.
- **A month value far from workdays × daily hours** means a full month of vacation does not reach the target of that month.
- **Changing a contract template changes every employee assigned to it**; for one person, assign a different contract.
- **Changes queue a recalculation of open entries**: contract, assignment, scheduling-rule and monthly-table changes, and changes of the company guaranteed hours, full time or daily hours (within the span of unlocked work entries). Sealed days are never touched.

### Trigger phrases

- "Wie werden die Sollstunden von Anna Müller berechnet?"
- "Why does Hans Muster have 184 target hours in January?"
- "Welcher Weg passt für ein 60-%-Pensum mit wechselnden Monatswerten?"
- "Where do the target hours of an employee without a contract come from?"

<!-- level:pitfalls -->

## Stage 4 — Pitfalls and what to tell the administrator

- **No contract means 100 %.** The company guaranteed hours and full time do not form a share; a part-time employee needs a contract.
- **Weg 1 with fixed hours is a contradiction** to clarify: percent or hours, not both.
- **A weekly or two-weekly contract inheriting a month value** gets that month value per weekly period in the period hours; keep inheriting contracts on a monthly interval.
- **The view interval hides contracts at booking time**, never for the period target - a weekly contract in a monthly group looks like "no contract" only for absences.
- **Target hours are time, never money.** Klacks plans time; payroll is somewhere else.
