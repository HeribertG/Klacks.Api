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

Marking a person as a legal entity switches the whole set of required fields: a company name and a
full address become mandatory and the type is locked to customer, while unmarking it demands a
first name, last name and a personal salutation instead.

<!-- level:short -->

## Company or person

Every contact in Klacks is either a real person or a company. A company is marked as a legal entity:
then Klacks asks for the company name and a full address, and the contact is always a customer —
a company cannot work as staff. A real person needs a first and a last name and can be an employee, an
external employee or a customer. Example: "Müller AG" is a legal entity and therefore a customer; "Anna
Müller" is a person and can be an employee.

<!-- level:elements -->

## Marking a contact as a legal entity

1. Open the contact (new or existing) and find the checkbox (de: "Juristische Person", en: "Legal Entity",
   fr: "Personne morale", it: "Entità legale").
2. Tick it. The type switches to (de: "Kunde", en: "Customer", fr: "Client", it: "Cliente") and the
   salutation to the legal-entity value at once, before anything is saved.
3. Enter the company name and make sure at least one address carries a postal code, a town and a country.
4. Save. The server checks the rules on every save and answers with a message if something is missing:
   "Company is required for legal entities", "Legal entities must be of type 'Customer'" or "Zip, city
   and country are required for legal entities".

To turn a company back into a person, untick the checkbox: the type returns to employee and the
salutation to female. Then enter first name and last name; without them the save is refused with "First
name is required" or "Last name is required" (and "Gender is required" if the salutation is not one of
the three personal values).

<!-- level:effects -->

## What the legal-entity mark controls

There is no in-between — each side has its own extra requirements on top of what every contact needs
anyway (an address, among other things):

- **Marked as a legal entity:** the company name is required, the type is locked to **customer**, and
  at least one of the contact's addresses must carry a postal code, town and country.
- **Not marked:** first name and last name are required instead, the type is free to be staff or
  customer, and the salutation must be one of the three personal values rather than the
  legal-entity value.

This is enforced on the server every time the record is saved, not just suggested by the form.

Customers are not rows in the schedule — the schedule lists only people who are not customers — so a
company never appears in the planning grid.

### Related skills

- `create_employee` / `update_client` — creating or changing a person, including the legal-entity flag
- `update_client_type` — changing the type directly (refuses this while legal entity is set)
- `update_client_gender` — the salutation field

### Trigger phrases

- "Warum kann ich diese Firma nicht als Mitarbeiterin anlegen?"
- "Ich habe das Häkchen bei juristischer Person gesetzt und plötzlich steht da Kunde."
- "Why does the type keep switching to customer?"
- "Do I have to enter a company name for this person?"

<!-- level:pitfalls -->

## Edge cases

**The checkbox rewrites type and salutation, and forgets the old values.** Ticking sets the type to
customer and the salutation to the legal-entity value; unticking sets the type to employee and the
salutation to female. Whatever was chosen before is not remembered, so somebody who was an external
employee and ticks and unticks the box ends up as an employee, not as external again.

**Changing the type away from customer is refused while the mark is set.** The type cannot be moved to
staff or external staff by itself; the mark has to be removed first, which in turn makes first name, last
name and a personal salutation mandatory. There is no back door that leaves a company classified as staff.
Klacksy's `update_client_type` says so in plain words: the legal-entity flag has to change first.

**One complete address is enough.** The postal-code, town and country rule is met when any one address of
the contact has all three; the other addresses may stay incomplete.

**The form can be inconsistent until saved.** The automatic switch happens in the form only; the checks
run when the record is saved.
