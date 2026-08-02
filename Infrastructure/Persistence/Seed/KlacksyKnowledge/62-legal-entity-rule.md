---
name: explain_legal_entity_rule
description: |
  Explains why marking a person as a legal entity locks their record to customer status: a company
  name becomes mandatory, the type must be customer, and an address with postal code, town and
  country is required. Removing the mark reverses this, requiring a first and last name plus a
  personal salutation instead. Use this when a company cannot be saved as staff, or when a
  checkbox unexpectedly changes the type or salutation shown.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - legal entity
  de:
    - juristische person
    - juristische person pflichtfelder
  en:
    - legal entity rule
    - legal entity must be customer
synonyms:
  de: [juristische person kunde pflicht, firma als mitarbeiter anlegen, warum firma nicht als mitarbeiter, checkbox juristische person, typ springt auf kunde, juristische person ankreuzen, gesellschaft als kunde]
  en: [legal entity forces customer type, company as an employee record, why can't i save a company as staff, legal entity checkbox effect, type jumped to customer, ticking legal entity changes gender]
  fr: [personne morale doit être client, entreprise comme employé impossible, case personne morale, type passe automatiquement à client]
  it: [persona giuridica deve essere cliente, azienda come dipendente impossibile, casella persona giuridica, tipo passa automaticamente a cliente]
---

# Legal entity — why a company can only ever be a customer

## Core idea (one sentence)

Marking a person as a legal entity switches the whole set of required fields: a company name and a
full address become mandatory and the type is locked to customer, while unmarking it demands a
first name, last name and a personal salutation instead.

## The two rule sets

A person is either a legal entity or not — there is no in-between, and each side has its own extra
requirements on top of what every person needs anyway (an address, among other things):

- **Marked as a legal entity:** the company name is required, the type is locked to **customer**,
  and at least one of the person's addresses must additionally carry a postal code, town and
  country.
- **Not marked:** first name and last name are required instead, the type is free to be staff or
  customer, and the salutation must be one of the three personal values rather than the
  legal-entity value.

This is enforced on the server every time the record is saved, not just suggested by the form.

## The surprise: the checkbox changes things by itself

Ticking the legal-entity checkbox does not just unlock the company field — it immediately sets the
type to **customer** and the salutation to the legal-entity value, before anything is saved.
Unticking it does the reverse: type flips back to staff and the salutation to female. Whatever type
or salutation was chosen before is not remembered — the switch always lands on the same fixed pair
of values in each direction.

Because of this, somebody who already picked "staff" and then ticks the box will find the type has
silently become customer. That is intended: a legal entity is a company, and Klacks only plans staff
and external staff on behalf of customers, never on behalf of a company record itself. Changing the
type away from customer afterwards is refused for as long as the legal-entity mark stays set — the
mark has to be removed first, so there is no back door that leaves a company classified as staff.

## Related skills

- `create_employee` / `update_client` — creating or changing a person, including the legal-entity flag
- `update_client_type` — changing the type directly (refuses this while legal entity is set)
- `update_client_gender` — the salutation field

## Trigger phrases

- "Warum kann ich diese Firma nicht als Mitarbeiterin anlegen?"
- "Ich habe das Häkchen bei juristischer Person gesetzt und plötzlich steht da Kunde."
- "Why does the type keep switching to customer?"
- "Do I have to enter a company name for this person?"
