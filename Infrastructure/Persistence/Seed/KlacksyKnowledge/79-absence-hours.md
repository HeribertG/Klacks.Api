---
name: explain_absence_hours
description: |
  Explains how many hours a paid absence day (vacation, accident, military service) credits and which
  master data decides it: daily working hours times the workload share, the 0-hours gate, weekend and
  holiday days, the override order scheduling rule before contract before company settings, what happens
  without a contract (full workload) or with another payment interval, directly recorded durations, and why existing
  entries keep their old hours until a recalculation. Use this when somebody asks why an absence day counts
  a certain number of hours or where to change it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - ferienstunden
    - urlaubsstunden
    - stunden pro ferientag
    - wie werden absenzstunden berechnet
  en:
    - vacation hours
    - hours per vacation day
    - how are absence hours calculated
synonyms:
  de: [wie viele stunden zählt ein ferientag, warum zählt der ferientag so viele stunden, wo stelle ich die ferienstunden ein, pensum für absenztage]
  en: [how many hours does a vacation day count, why does the vacation day count that many hours, where do I set the vacation hours, workload for absence days]
  fr: [combien d'heures compte un jour de vacances, où régler les heures de vacances, taux d'occupation pour les absences]
  it: [quante ore conta un giorno di ferie, dove impostare le ore di ferie, percentuale di impiego per le assenze]
---

# Absence hours — how many hours a vacation day credits and where that is set

## Core idea (one sentence)

A paid absence day credits the **daily working hours times the workload share** of the employee on that day (0 on weekend days, holidays and for 0 guaranteed hours); the share comes from the **contract**, and without a contract it is 100 %, so the contract is the place to set it.

<!-- level:short -->

## Stage 1 — What it is and what it is for

When an absence such as vacation is booked, Klacks does not store "one day"; it stores hours, because the hours count towards the guaranteed hours of the period. Every absence type has a calculation rule (a macro) that works the hours out from the real context of that day: the regular daily hours of the employee, the workload share and whether the day is a weekend day or a public holiday. For vacation this is simply daily hours × workload share, so a full-time employee with 8 daily hours gets 8 h, an 80 % employee gets 6.4 h, and a Saturday gets 0 h. Hours are time credits, never money.

<!-- level:elements -->

## Stage 2 — The calculation step by step

### Step 1 — the daily hours

The starting point is the daily working hours (de: "Tägliche Arbeitsstunden", en: "Default Working Hours") from the company settings, section (de: "Arbeitszeit & Planung", en: "Work & Scheduling"). A contract has no such field; only a scheduling rule attached to the contract can override it with its own (de: "Tägl. Arbeitsstunden", en: "Daily Working Hours").

### Step 2 — the workload share

- **Contract with fixed guaranteed hours:** share = guaranteed hours ÷ full time × 100, both read from the contract (de: "Garantierte Stunden", en: "Guaranteed Hours" and de: "Vollzeit", en: "Full Time"); a full time of 0 falls back to the company full time. A scheduling rule that sets guaranteed hours or full time wins over the contract's own fields (the rule's guaranteed hours only on this path - an inheriting contract ignores them).
- **Contract that reads the monthly table** — Weg 1: payment interval (de: "Monatlich (Sollstunden-Tabelle)", en: "Monthly (target hours table)") plus the percent field; Weg 2: guaranteed hours left empty plus the percent field (de: "Pensum (%)", en: "Workload (%)"): share = that percent, 100 when empty. The monthly value is the target time of a 100 % position for that month; the guaranteed hours become monthly value × percent (Weg 1 in a month without a row: the contract's own hours; Weg 2: the company guaranteed hours × percent).
- **No contract:** share = 100 %, exactly like an inheriting contract with an empty percent field; the guaranteed hours are the monthly value of the table (de: "Monatliche Sollstunden", en: "Monthly target hours") or, without a row, the company guaranteed hours (de: "Garantierte Stunden", en: "Guaranteed Hours" of the work settings). Any other workload share needs a contract.
- Implausible contract bases (no positive full time, or a share above 150 %) fall back to 100 % and are logged.

### Step 3 — the day

Weekend days (the company's configured weekend) and official holidays of the employee's holiday calendar credit 0 h. If the effective guaranteed hours are 0 (an on-call style contract), the day credits 0 h as well. Otherwise: daily hours × share ÷ 100. Some absence types apply their own rule on top (half vacation halves the result; a paid absence with recorded times credits the recorded span).

### Step 4 — override order, in one line

Scheduling rule → contract → company settings; the monthly target hours row cuts into this chain for the guaranteed hours only.

### Step 5 — where to set what

| Value | Preferred place | Fallback |
|---|---|---|
| Workload share | contract: guaranteed hours and full time, or the percent field (settings page de: "Verträge", en: "Contracts") | company work settings |
| Daily hours | scheduling rule of the contract (de: "Planungsregeln", en: "Scheduling Rules") | company work settings |
| Hours of one month for everybody | table (de: "Monatliche Sollstunden", en: "Monthly target hours") | — |
| Which contract applies to a person | contracts card of the person (de: "Verträge", en: "Contracts"), button (de: "Vertrag hinzufügen", en: "Add Contract") | company settings when none is assigned |

Prefer the contract: it is assigned per employee, it is visible, and it is what the booking reads first. A contract is a template shared by everybody assigned to it - changing its hours changes every one of them; for one person, assign a different contract.

### Step 6 — see the real figures

`get_absence_hours_calculation` runs the real calculation for an employee and a day and names the source of every figure; `evaluate_absence_hours_setup` adds what to change where when the result is not the one wanted. Changing a contract (`update_contract`) or assigning one (`assign_contract_by_name`) is a separate step; the system asks for one confirmation before it is written, so do not ask a separate "shall I?" first.

<!-- level:effects -->

## Stage 3 — What the choices cause

### Payment interval

An absence booked in the schedule is calculated with the payment interval of the view (the selected group's interval, otherwise the company interval). The two calendar-month intervals count as one: a view on (de: "Monatlich", en: "Monthly") also sees contracts on (de: "Monatlich (Sollstunden-Tabelle)", en: "Monthly (target hours table)") and vice versa. A contract on any other interval (weekly, every two weeks, individual) is **not seen** by a monthly view: the employee is treated as having no contract for that booking, and the company value applies at 100 %. A recalculation of the period, however, reads the contract regardless of interval. Keep the contract on the interval the schedule works with. If an employee holds a monthly and a monthly-target-hours contract from the same start day, the one created last applies.

### Changes afterwards

Existing absence entries keep the hours they were booked with. A change of the company guaranteed hours, full time or daily hours, a change of a contract's hours or an assignment, and a change of a monthly target hours row queue a recalculation — but only over the span of the unlocked work entries (a monthly row: its month); where no work entries exist, nothing is recalculated. Changing only a contract's percent field, or a scheduling rule's hour fields, triggers nothing. To be sure, run (de: "Stunden berechnen und zusammenzählen", en: "Calculate and sum hours") in the schedule toolbar for the period: it recomputes every open absence entry and skips confirmed, approved and closed ones.

### Copy and move

Copying, filling or moving an absence creates a new entry, so the macro runs again for the **target** employee and day — a vacation copied to somebody with 0 guaranteed hours credits 0 h. An absence recorded as a plain duration keeps its duration when copied or moved (the recorded hours travel with it; since 2026-10-25).

### Directly recorded duration

An extended absence entered as a duration carries its hours directly (equal start and end time, hours above 0); no macro overwrites it, not even a recalculation.

### Trigger phrases

- "Why does Anna Müller get 7.56 hours per vacation day?"
- "Wie werden die Ferienstunden berechnet und wo stelle ich das ein?"
- "How many hours does a vacation day count for a part-time employee?"
- "Wo lege ich das Pensum für Absenztage fest?"

<!-- level:pitfalls -->

## Stage 4 — Pitfalls and what to tell the administrator

- **No contract means 100 %.** The company guaranteed hours and full time of the settings do not form a share; a part-time share needs a contract (fixed hours, or a percent with Weg 1 or Weg 2).
- **A partly filled monthly table switches the basis from month to month.** Employees without a contract and contracts on Weg 1 or Weg 2 take the monthly value where a row exists and the settings or contract value elsewhere; fill every month, or use a contract with fixed hours. A monthly value far from workdays × daily hours means a full month of vacation does not reach the month's target.
- **The percent field is ignored on a contract with fixed guaranteed hours.** It scales only an inherited or monthly basis; with fixed hours the ratio guaranteed hours ÷ full time applies. A contract that states both can contradict itself.
- **A scheduling rule silently overrides the contract.** If the rule sets full time or daily hours, those win; its guaranteed hours win on a contract with fixed hours (an inheriting contract keeps the company value). The contract page shows the rule under (de: "Planungsregeln", en: "Scheduling Rules").
- **0 guaranteed hours means 0 absence hours by design.** This is the on-call case, not an error.
- **The payment interval of the view decides whether the contract is seen.** Monthly and monthly-target-hours contracts are both seen by a monthly view; a weekly, two-weekly or individual contract is ignored by it at booking time.
- **Old entries do not follow new master data by themselves.** Recalculate the period, and reopen sealed days first.
- **Surcharges and absence hours are time, never money.** Klacks plans time; payroll is somewhere else.
