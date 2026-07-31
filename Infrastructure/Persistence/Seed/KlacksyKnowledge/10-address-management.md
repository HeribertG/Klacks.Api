---
name: explain_address_management
description: |
  Explains the data model behind people in Klacks: the three kinds of person (own staff, external
  staff, customer), what a person needs at minimum before they can be saved, the three address kinds,
  and why addresses are versioned by date rather than overwritten. Use this when the user asks what
  is mandatory when creating someone, why an old address is still visible, what the difference
  between a workplace and an invoicing address is, or why a person needs an email address.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - pflichtfeld
  - mindestens
  - mitgliedschaft
  - externe mitarbeiter
  - rechnungsadresse
  - arbeitsort
synonyms:
  de: [pflichtfelder, was braucht eine person, mindestangaben, mitgliedschaft, externer mitarbeiter, auftraggeber, rechnungsadresse, arbeitsort, alte adresse, adresse historisiert]
  en: [mandatory fields, what does a person need, membership, external employee, customer, invoicing address, workplace address, old address, historized address]
  fr: [champs obligatoires, adhésion, employé externe, client, adresse de facturation, lieu de travail, ancienne adresse]
  it: [campi obbligatori, appartenenza, dipendente esterno, cliente, indirizzo di fatturazione, luogo di lavoro, vecchio indirizzo]
---

# People in Klacks — the model behind the address list

## Core idea (one sentence)

A person is never stored alone: they always come with an address, contact details and a membership
that says from when to when they belong to the company.

## Three kinds of person

- **Own staff** — employed by the company.
- **External staff** — working for the company without being employed by it.
- **Customer** — the client an assignment is carried out for.

The business rule behind the distinction: Klacks plans **own and external staff** on behalf of
**customers**. That is why the kind is not cosmetic — it decides whether someone can be scheduled or
is the reason a schedule exists.

## What is mandatory

A person cannot be saved without an **address** and a **membership**. Contact details are strongly
recommended rather than enforced, for a practical reason:

- The **membership** sets the time frame — from when, and optionally until when. It also supplies
  the earliest date any group or contract assignment can start from.
- Without an **email address** there is no planning by email, and the person cannot write to Klacks
  either. A phone number is worth asking for at the same time.
- The address **should exist in reality**. Klacks checks it geographically when a map service is
  configured.

## Three kinds of address

- **Home address** of a member of staff.
- **Workplace** — where the work happens; belongs to the customer.
- **Invoicing address** — where the bill goes; belongs to the customer. Staff never have one.

An address holds street, postal code, town, region and country. For Swiss postal codes, town and
canton are filled in automatically.

## Why old addresses stay

Addresses are **versioned by date, not overwritten**. Each one carries a date from which it applies,
and the address in force on any given day is the most recent one starting on or before it. Somebody
who moved in March still has their old address on a January assignment — which is exactly what an
old schedule or an old invoice needs.

That is why correcting a typo and recording a move are two different operations: the first fixes the
existing entry, the second adds a new one with a new start date.

## What else hangs off a person

- **Groups** — structure for both staff and customers.
- **Contract** — working conditions and holiday calendar, staff only. See `explain_contracts`.
- **Notes** — free annotations.

## Related skills

- `create_employee` / `update_client` / `delete_client` — the person themselves
- `create_address` / `update_address` — addresses, including recording a move
- `add_client_email` / `add_client_phone` / `update_communication` — contact details
- `list_client_memberships` / `update_membership` / `end_client_membership` — the time frame
- `validate_address` — geographic check

## Trigger phrases

- "What do I have to fill in to create a person?"
- "Why does the old address still show up?"
- "What is the difference between a workplace and an invoicing address?"
- "Why does everyone need an email address?"
- "Was ist ein externer Mitarbeiter?"
