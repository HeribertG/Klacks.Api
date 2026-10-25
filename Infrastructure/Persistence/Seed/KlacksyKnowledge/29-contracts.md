---
name: explain_contracts
description: |
  Explains the working conditions a person is employed under: a contract template carries guaranteed,
  minimum, maximum and full-time hours, night/holiday/Saturday/Sunday surcharges, the working
  weekdays, a payment interval and optionally its own public-holiday calendar. Covers the difference
  between a template and its assignment to a person, which contract counts when several exist, and
  what applies when somebody has none. Use this when the user asks about working hours owed, weekly
  hours, surcharge rates, part-time percentages, expiring contracts, or why a person's target hours
  look wrong.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - vertrag
  - contract
  - sollstunden
  - pensum
  - arbeitszeit
  - zuschlag
  - vollzeit
  - teilzeit
synonyms:
  de: [vertrag, vertragsvorlage, arbeitsvertrag, sollstunden, pensum, garantierte stunden, vollzeit, teilzeit, zuschlag, zahlungsintervall, vertrag läuft ab]
  en: [contract, contract template, target hours, guaranteed hours, full time, part time, surcharge, payment interval, expiring contract]
  fr: [contrat, modèle de contrat, heures dues, heures garanties, temps plein, temps partiel, supplément, contrat expire]
  it: [contratto, modello di contratto, ore dovute, ore garantite, tempo pieno, tempo parziale, supplemento, contratto scade]
---

# Contracts — the working conditions behind a person

## Core idea (one sentence)

A contract is a **template** describing working conditions; it only takes effect once it is assigned to a person, and several people can share the same template.

<!-- level:short -->

## Stage 1 — What it is and what it is for

A contract says under which working conditions a person works: how many hours they are meant to work, on which weekdays, and which extra time credits apply for night, Saturday, Sunday or public-holiday work. It is written once as a template and then handed to every person it fits, so ten part-timers can share one template. The hours a person is meant to work (their target hours) come from the contract that is currently active for them. Example: the template "Part time 80 %" is assigned to Maria from 1 January, and from that day Maria's target hours follow it. A person without any contract is not left without conditions — the company-wide value applies at 100 % (the monthly value of the table for that month, otherwise the company guaranteed hours) — but any other workload share needs a contract; `explain_target_hours` has the full guide.

<!-- level:elements -->

## Stage 2 — How to do it

### Give a person a contract

Contracts exist for own and external staff; customers have no contract card. Changing assignments needs the right to edit contracts (Admin and Supervisor); an account without a role can look but not change.

1. Open the person (list de: "Alle Adressen", en: "All Addresses", pencil in the row) and go to the card (de: "Verträge", en: "Contracts").
2. Click (de: "Vertrag hinzufügen", en: "Add Contract"): a new row appears.
3. Pick the template in the column (de: "VERTRAG", en: "CONTRACT"), set (de: "GÜLTIG VON", en: "VALID FROM") and, if the assignment is limited, (de: "GÜLTIG BIS", en: "VALID UNTIL"). The date picker of the start day does not offer days before the person's entry date.
4. Tick (de: "AKTIV", en: "ACTIVE") on the row that should count, then save the person with (de: "Eingaben speichern", en: "Save entries").

Ticking one row as active unticks all others. If rows exist but none is active, a red hint says (de: "Mindestens ein Vertrag muss aktiv sein", en: "At least one contract must be active") and the person cannot be saved.

### What a template holds

The templates themselves are maintained in the contract settings page.

**Hours** — guaranteed, minimum, maximum and full-time, each as hours and minutes. Guaranteed, minimum and maximum hours may not be negative, the minimum may not exceed the maximum, and the guaranteed hours must lie between the minimum and the maximum.

**Surcharges** — night, public holiday, Saturday and Sunday as percentages from 0 to 100. These sit on the contract; an empty value means the standard applies.

**Working weekdays** — which days this contract generally foresees work on. Monday to Friday are preset, Saturday and Sunday are not.

**Shift work** — a flag marking contracts for people on early, late or night duty.

**Validity** — valid from is mandatory, valid until is optional, so a contract can be open-ended or time-limited; the end must lie after the start.

**Payment interval** — weekly, every two weeks, monthly, individual, or *monthly target hours*.

**Public holiday calendar** — each contract can carry its own, differing from the company-wide one. That is how regionally different public holidays are handled for people working in different areas.

**Planning rule** — an existing rule can optionally be attached.

<!-- level:effects -->

## Stage 3 — How it fits together

### Template versus assignment

The template alone changes nothing. Assigning it to a person is a separate step with its own validity dates. Only one assignment is active per person at a time: ticking a new one deactivates the others, and the server keeps only the last active one if several arrive.

### Which values apply

For a given day only the **active** assignment whose dates contain that day counts; if several overlap, the one with the latest start wins. Its values are resolved per field:

- Weekdays, daily hours and similar limits come from an attached **planning rule** first, then the **contract**, and finally the **company-wide defaults**. The guaranteed hours follow that order only for a contract with fixed hours; a contract that inherits the company value (empty guaranteed hours) or sits on the interval "monthly target hours" with a month row takes the company value × percent and ignores the rule's guaranteed hours.
- The **surcharge** rates resolve contract first: an explicit value on the contract, also 0, wins; an empty value means "standard", which is the rule and then the company-wide setting. While the shift-work flag is effectively off, the contract's own rates are skipped and the standard applies.
- A person without a matching contract takes the company-wide monthly value for that month when the monthly table has a row for it, otherwise the company-wide setting — always at 100 %, never scaled.

### Target hours

The target hours of a pay period are **not stored**: the schedule recalculates them from the contract on every read.

- **Fixed hours** — a contract with explicit guaranteed hours gives exactly those hours (or those of the attached rule). The workload percent does **not** scale them.
- **Inherited workload** — a contract without guaranteed hours takes the company-wide value (the monthly table row, else the setting) times the workload percent; no percent means 100.
- **Monthly target hours** — with this payment interval a company-wide table of hours per calendar month decides every month that has a row there, times the percent. Such a row wins even over a planning rule.
- When the membership of the person starts or ends inside the pay period, the target is scaled by the member days.

### Changes are propagated

The displayed target hours are computed on read and need no step. The stored hours of work and absence entries are recalculated when a change is saved — an assignment added, removed, re-dated or re-bound, a contract template's hours or rates, a scheduling rule, a row of the monthly table, or the company guaranteed hours, full time or daily hours — over the span of the unlocked entries of the affected people; sealed days are never touched. A change without any work entries in that span queues nothing; the recalculation in the schedule toolbar (de: "Stunden berechnen und zusammenzählen", en: "Calculate and sum hours") does it on demand.

### Related skills

- `list_contracts` / `get_contract_details` — which templates exist and what is in one
- `create_contract` / `update_contract` / `delete_contract` — maintain templates
- `assign_contract_to_client` / `assign_contract_by_name` — give a person a contract; the new one becomes the active one and the previous active one is deactivated
- `list_expiring_contracts` — assignments running out soon, 90 days ahead unless asked otherwise; staff only, and only assignments that are still active

### Trigger phrases

- "How many hours does this person owe per week?"
- "What is the difference between guaranteed and maximum hours?"
- "Which contracts expire soon?"
- "What happens if someone has no contract?"
- "Warum stimmen die Sollstunden nicht?"

<!-- level:pitfalls -->

## Stage 4 — Consequences, edge cases, shortcuts

### Active means "counts"

- Only active rows count, and ticking a new one deactivates the old one. For days before the new contract's start day no active assignment matches any more, so the company-wide defaults apply there. Check the target hours of earlier periods after replacing a contract.
- A person with no assignment at all can be saved, but then everything depends on the company-wide settings and the monthly table.
- If rows exist, at least one must be active; otherwise the save is refused.

### Dates

- The date picker offers no start before the entry date. The server does not check this separately, and a contract starting earlier does not make the person plannable earlier: planning is bounded by the membership, not by the contract.
- The target hours of a pay period use the contract that is active on the **first day** of that period. A contract that starts in the middle of a pay period does not change that period's target.

### Target hours look wrong — quick checks

1. Is exactly one assignment active, and does it cover the first day of the period?
2. Does the contract have explicit guaranteed hours? Then the percent is ignored.
3. Is an attached planning rule setting hours? A rule wins over the contract's own values; the one exception are guaranteed hours of a contract that inherits them or uses monthly target hours.
4. Does the monthly table have a row for that month? It overrides the contract for inheriting and monthly-target-hours contracts.
5. Did the person join or leave inside the period? The target is scaled by the member days; the maximum hours are never scaled.

### Final versus correctable

- Removing a row in the contract card and saving removes the assignment; re-add it with its dates if that was a mistake.
- Do not delete a template that people still hold; end it with a valid-until date instead.
- Surcharges are time credits, not money. An explicit 0 on the contract means "no credit", not "use the standard".
- An explicit guaranteed value of 0 is an on-call contract and stays 0.
