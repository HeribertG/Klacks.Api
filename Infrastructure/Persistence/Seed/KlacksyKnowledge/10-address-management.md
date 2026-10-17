---
name: explain_address_management
description: |
  Explains the data model behind people in Klacks: the three kinds of person (own staff, external
  staff, customer), what a person needs at minimum before they can be saved, the three address kinds,
  and why addresses are versioned by date rather than overwritten. Use this when the user asks what
  is mandatory when creating someone, why an old address is still visible, what the difference
  between a workplace and an invoicing address is, which address kinds a kind of person may have,
  or why a person needs an email address.
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

A person always comes with an address and a membership (from when to when they belong to the company); contact details are strongly recommended on top.

<!-- level:short -->

## Stage 1 — What it is and what it is for

Everybody Klacks plans, or plans for, is a person in the address list. There are three kinds: **own staff** (employed by the company), **external staff** (they work for the company without being employed by it) and **customers** (the clients the work is done for). Klacks schedules staff on behalf of customers, so the kind matters: staff get scheduled, customers are the reason a schedule exists. To add someone you need their name, a real address and the day they join; an email address is strongly recommended, because without one there is no planning by email and the person cannot write to Klacks. When somebody moves you do not overwrite the old address, you add the new one with the day it starts. Example: Anna moves on 1 March — for a day in January her old address still applies, for April her new one.

<!-- level:elements -->

## Stage 2 — How to do it in the app

### Create a new person

Needs the right to create persons (Admin and Supervisor); an account without a role can only look.

1. Open the list (de: "Alle Adressen", en: "All Addresses") and click (de: "+ Adresse erfassen", en: "+ Add Address").
2. The empty mask opens with defaults: type Employee, entry date today, the next ID number and a main address valid from today.
3. Fill in the salutation (de: "Anrede", en: "Salutation"), first name (de: "Vorname", en: "First Name") and last name (de: "Nachname", en: "Last Name"). For a legal entity tick (de: "Juristische Person", en: "Legal Entity") and fill (de: "Firma", en: "Company") instead; a legal entity can only be saved as a customer.
4. Fill the address: (de: "Strasse", en: "Street"), (de: "PLZ", en: "ZIP"), (de: "Ort", en: "City") and (de: "Nation", en: "Country"). ZIP, city and country are required; for a Swiss postal code town and canton are suggested automatically.
5. In the card (de: "Registration", en: "Registration") choose the kind of person in (de: "Typ", en: "Type") and check (de: "Eintrittsdatum", en: "Entry Date"), which is required.
6. Add phone and email on the right, then save with (de: "Eingaben speichern", en: "Save entries"). The mask only accepts the save when the required fields are filled in.

While you type, similar existing persons appear in the right-hand list (de: "Gefunden", en: "Found") as a duplicate hint.

### Record a move (a new address version, not a new person)

1. Open the person and, in the header of the first card, open the options menu and choose (de: "Neue Adresse erstellen", en: "Create new address").
2. In the dialog (de: "Neue Adresse", en: "New Address") set (de: "Adresse gültig ab", en: "Address valid from") — preset to today — and, where the kind of person allows several, the address kind in (de: "Adressart", en: "Address Type"). Confirm with (de: "Eingabe speichern", en: "Save Entry").
3. An empty address entry appears. Fill it in and save the person with (de: "Eingaben speichern", en: "Save entries").
4. The right-hand column lists the entries as (en: "Current Addresses"), (en: "Future addresses") and (en: "Past Addresses"); a click shows that version in the card.

### Correct a typo instead

Edit the address shown in the card directly and save: that fixes the existing entry and creates no new version.

### Change the kind of person, record an exit, remove an address

- Kind of person: dropdown (de: "Typ", en: "Type") in the card (de: "Registration", en: "Registration").
- Exit: fill (de: "Austrittsdatum", en: "Exit Date") in the same card and save.
- Remove an address version: options menu entry (de: "Adresse deaktivieren", en: "Deactivate Address").

### Delete a person

Only Admin sees the red trash icon in the list row; a confirmation dialog follows.

<!-- level:effects -->

## Stage 3 — How it fits together

### Three kinds of person

- **Own staff** — employed by the company.
- **External staff** — working for the company without being employed by it.
- **Customer** — the client an assignment is carried out for.

Klacks plans **own and external staff** on behalf of **customers**. The kind is not cosmetic: it decides whether someone can be scheduled or is the reason a schedule exists. Customers have no contract and no qualification card in the mask.

### What is mandatory

A person cannot be saved without an **address** and a **membership**. Contact details are recommended rather than enforced:

- The **membership** sets the time frame — from when, and optionally until when. Its entry date is also the earliest day the date pickers of the group and contract cards offer.
- Without an **email address** there is no planning by email, and the person cannot write to Klacks either. A phone number is worth asking for at the same time.
- The address **should exist in reality**. Klacks checks it geographically when a map service is configured.

### Three kinds of address

- **Main address** (Hauptadresse) — the home address of a member of staff, or the main address of a customer.
- **Business address** (Geschäftsadresse, workplace) — where the work happens.
- **Invoicing address** (Rechnungsadresse) — where the bill goes.

Which kinds a person may have depends on the kind of person, and Klacks enforces it when saving:

| Kind of person | Allowed address kinds |
|---|---|
| Own staff | main address only |
| External staff | main address and business address |
| Customer | main, business and invoicing address |

Addresses saved before this rule existed stay editable, but a new address of a disallowed kind is rejected. An address holds street, postal code, town, region and country.

### Why old addresses stay

Addresses are **versioned by date, not overwritten**. Each one carries a date from which it applies, and the address in force on any given day is the most recent one starting on or before it. Somebody who moved in March still has their old address on a January assignment — which is exactly what an old schedule or an old invoice needs. That is why correcting a typo and recording a move are two different operations: the first fixes the existing entry, the second adds a new one with a new start date. In the mask the entry shown as current is picked separately for each address kind: the newest version of that kind starting on or before today; older ones of the same kind count as past, later ones as future.

### What else hangs off a person

- **Groups** — structure for both staff and customers.
- **Contract** — working conditions and holiday calendar, staff only. See `explain_contracts`.
- **Notes** — free annotations.

### Where the membership reaches

The entry and exit dates are the planning window of a person: the schedule and the absence calendar accept entries only inside it, and the target hours of a pay period are scaled when the window starts or ends inside it. Details are in the last stage.

### Related skills

- `create_employee` / `update_client` / `delete_client` — the person themselves
- `create_address` / `update_address` — addresses, including recording a move
- `add_client_email` / `add_client_phone` / `update_communication` — contact details
- `list_client_memberships` / `update_membership` / `end_client_membership` — the time frame
- `validate_address` — geographic check

### Trigger phrases

- "What do I have to fill in to create a person?"
- "Why does the old address still show up?"
- "What is the difference between a workplace and an invoicing address?"
- "Why does everyone need an email address?"
- "Was ist ein externer Mitarbeiter?"

<!-- level:pitfalls -->

## Stage 4 — Consequences, what is final, edge cases

### A new person is missing from the list

The list opens with the filter (de: "Gültigkeit", en: "Validity") on (de: "Aktive", en: "Active") only. A person whose entry date lies in the future appears only under (de: "Zukünftige", en: "Future"), one whose exit date has passed only under (de: "Ehemalige", en: "Alumni"). Deleted persons appear only when (de: "Gelöschte Adressen", en: "Deleted Addresses") is ticked, and then the list shows **only** deleted persons.

### Move, typo, removal

- A typo fix edits the stored entry itself, so it also changes what past days show. Use a new version for a real move.
- A new version with a future start day is a scheduled move: it stays in the future list until that day arrives.
- (de: "Adresse deaktivieren", en: "Deactivate Address") sets no end date. It takes the version out of the person, and the removal is written when the person is saved. Before saving, (de: "Zurücksetzen", en: "Reset") in the bottom bar discards all pending changes of the mask.

### Changing the kind of person

- The dropdown accepts the new kind without any check; the save is rejected if the person still has an address kind the new kind does not allow (for example a business address on own staff). Remove those versions first, then save again.
- A legal entity can only be a customer; switching it to staff is rejected on save.
- Switching to customer removes the contract and qualification cards from the mask. What happens to contracts already stored was not verified.
- Expiry sweeps for contracts and the target-hours drift check cover staff only, never customers.

### Geocoding failures

- The check runs on save, only for addresses with postal code and town that changed since they were stored. Saving an unchanged address again does not retry it, so a missing location is only fixed by changing street, postal code, town or country.
- If the geocoder answers "not found", finds no exact street match or disagrees with the canton, the save stops. A hint offers suggestions and (de: "Trotzdem speichern", en: "Save anyway"). A chosen suggestion takes over street, postal code and town together with the coordinates. "Save anyway" skips the check for all addresses of that save and stores the entered values, normally without coordinates.
- If the geocoder is unreachable or switched off, nothing stops the save and no hint appears: the address is stored without coordinates.
- Without coordinates the external map buttons (OpenStreetMap, Street View) do not appear (they exist only for customers with coordinates, and only while external map links are enabled), the skill that adds a person to the nearest group changes nothing and says the person has no address with coordinates, and location-based grouping cannot place addresses outside its cluster towns.
- Klacksy's `create_address` goes through the address endpoint instead: postal code and town are required, an address the geocoder cannot find is rejected, and an unreachable geocoder is accepted without coordinates.

### Exit date

- The exit date is the **last day** of employment; planning is refused from the next day on. `end_client_membership` reports exactly that first non-plannable day.
- Absences must lie completely inside the membership window, and new work entries outside it are refused (also outside the group period of the person).
- The target hours of a pay period are scaled by the member days inside it: an exit on 15 March gives 15/31 of the monthly target. The maximum hours are never scaled.
- The exit date can be changed or cleared again in the same field; it is a correction, not a one-way step.
- Work entries that already exist after a newly set exit date are kept: Klacks neither deletes nor flags them, and saving the exit date gives no warning about them. In the schedule those days are locked in the person's row; in a period that lies completely after the exit the row is no longer listed, but the entries still exist. So check the person's planned entries before setting the exit date.

### Deleting a person

- Only Admin may delete; Supervisor and accounts without a role cannot.
- Deleting is a soft delete: the person is hidden from the normal list and stays visible only with (de: "Gelöschte Adressen", en: "Deleted Addresses"), where all cards open read-only. There is no restore function in the app, so treat it as final.
- Prefer an exit date over deleting whenever the person simply leaves.

### Rights in short

Admin can do everything on persons. Supervisor can view, create and edit but not delete. An account without a role can view persons, plan, and write notes, but not change master data.
