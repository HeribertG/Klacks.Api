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

A contract is a **template** describing working conditions; it only takes effect once it is assigned
to a person, and several people can share the same template.

## What a template holds

**Hours** — guaranteed, minimum, maximum and full-time, each as hours and minutes. The minimum may
not exceed the maximum, the guaranteed hours may not exceed the maximum, and none of them may be
negative.

**Surcharges** — night, public holiday, Saturday and Sunday as percentages from 0 to 100. These sit
on the contract and are independent of the company-wide planning defaults.

**Working weekdays** — which days this contract generally foresees work on. Monday to Friday are
preset, Saturday and Sunday are not.

**Shift work** — a flag marking contracts for people on early, late or night duty.

**Validity** — valid from is mandatory, valid until is optional, so a contract can be open-ended or
time-limited.

**Payment interval** — weekly, every two weeks, monthly, individual, or *monthly target hours*. The
last one is special: it makes a company-wide table of hours per calendar month override what the
contract would otherwise say, scaled by the person's percentage.

**Public holiday calendar** — each contract can carry its own, differing from the company-wide one.
That is how regionally different public holidays are handled for people working in different areas.

**Planning rule** — an existing rule can optionally be attached.

## Template versus assignment

The template alone changes nothing. Assigning it to a person is a separate step with its own validity
dates, and **only one contract is active per person at a time** — assigning a new one deactivates the
previous one.

## Which values actually apply

The values in force are resolved in a fixed order: an attached **planning rule** first, then the
**contract**, and finally the **company-wide defaults**. So a person without a contract is not
without conditions — they fall back to the defaults. That works for simple planning but is worth
avoiding, because everything then depends on one global setting.

## Two practical points

Contracts and qualifications that run out within the next three months can be listed in advance, so
an expiry does not surprise anyone mid-plan.

Deleting a template does **not** touch assignments already made. Where a contract should simply stop
applying, setting its valid-until date is the safer route than deleting it.

## Related skills

- `list_contracts` / `get_contract_details` — which templates exist and what is in one
- `create_contract` / `update_contract` / `delete_contract` — maintain templates
- `assign_contract_to_client` / `assign_contract_by_name` — give a person a contract
- `list_expiring_contracts` — assignments running out soon

## Trigger phrases

- "How many hours does this person owe per week?"
- "What is the difference between guaranteed and maximum hours?"
- "Which contracts expire soon?"
- "What happens if someone has no contract?"
- "Warum stimmen die Sollstunden nicht?"
