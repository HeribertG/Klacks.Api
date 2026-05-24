---
name: explain_address_management
description: |
  Explains how Klacks creates and connects clients via the address management (Adressverwaltung).
  Use this when the user asks about creating a client/employee/customer, addresses, what a client
  needs at minimum (address + membership), client types (Employee/ExternEmp/Customer), address
  types (Employee/Workplace/InvoicingAddress), communication (phone/email and why email enables
  email-based planning), groups, contracts (working conditions + holiday calendar), postal codes
  (PLZ), cantons (Kanton/state), countries, the "valid as of" (validFrom) scope, or address
  validation via the geocoding service (openrouteservice).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - address
  - adresse
  - client
  - kunde
  - mitarbeiter
  - employee
  - customer
  - membership
  - communication
  - email
  - telefon
  - contract
  - vertrag
  - group
  - gruppe
  - rechnungsadresse
  - arbeitsort
  - plz
  - kanton
synonyms:
  de: [adresse, kunde, mitarbeiter, kunde anlegen, client erfassen, membership, mitgliedschaft, kommunikation, email, telefon, vertrag, gruppe, rechnungsadresse, arbeitsort, plz, kanton, geocoding]
  en: [address, client, employee, external employee, customer, create client, membership, communication, email, phone, contract, group, invoicing address, workplace, postal code, canton, geocoding]
  fr: [adresse, client, employé, membre, communication, courriel, téléphone, contrat, groupe, adresse de facturation, lieu de travail, code postal, canton]
  it: [indirizzo, cliente, dipendente, appartenenza, comunicazione, email, telefono, contratto, gruppo, indirizzo di fatturazione, luogo di lavoro, cap, cantone]
---

# Adressverwaltung — Clients anlegen und vernetzen

## Kern-Idee (1 Satz)

Die Adressverwaltung ist in Klacks der **Einstiegspunkt, um Clients anzulegen** — und ein Client
entsteht nie allein: zusammen mit ihm werden Adresse, Kommunikation, Mitgliedschaft und oft auch
Vertrag, Gruppenzuordnung und Notiz erzeugt.

## Client-Typen (`EntityTypeEnum`)

- **Employee = 0** — eigener Mitarbeiter
- **ExternEmp = 1** — externer Mitarbeiter
- **Customer = 2** — Auftraggeber/Kunde

**Geschäftslogik:** Klacks erstellt Planungen für **Employee + ExternEmp** im **Auftrag von Customer**.

## Was ein Client mindestens braucht

Beim Erfassen gilt als **Minimum**: `client` + `address` + `communication` + `membership`.

- **Kein Client ohne Adresse und ohne Membership** — beides ist Pflicht.
- **Membership** setzt den zeitlichen Rahmen (gültig ab/bis) und liefert das früheste Datum für
  Gruppen- und Vertragszuordnungen.
- **Communication (Telefon + Email)** ist dringend erwünscht: **Ohne Email keine Planung via
  Email.** Mit Email kann der Client zudem **direkt an Klacks schreiben** (eingehende Mail).
- Die **Adresse sollte real existieren** — das Backend prüft sie geografisch, sofern ein gültiger
  **openrouteservice**-Key konfiguriert ist.

## Adress-Typen (`AddressTypeEnum`)

- **Employee = 0** — Adresse des Mitarbeiters
- **Workplace = 1** — Arbeitsort → Adresse des **Customers**
- **InvoicingAddress = 2** — Rechnungsadresse → Adresse des **Customers**
  (Mitarbeiter haben **nie** eine InvoicingAddress)

Adressen sind über `validFrom` zeitversioniert; die „im Scope"-Adresse ist die jüngste mit
`validFrom ≤ Stichtag`. Felder: `street`, `zip` (PLZ), `city`, `state` (Kanton), `country`
(+ Geo-Koordinaten). Bei CH-PLZ werden Stadt und Kanton automatisch ergänzt.

## Weitere Verknüpfungen

- **Group** — Struktur: Mitarbeiter (Employee/ExternEmp) **oder** Customer lassen sich in Gruppen
  einteilen. Wichtig, um Ordnung in die Bestände zu bringen.
- **client_contract** — weist einen **Contract** zu (Arbeitsbedingungen + **Kalender** für die
  Feiertagsregelung). Gilt **nur für Mitarbeiter**. Ohne Vertrag greift der **Default aus den
  Settings** — für einfache Planungen ausreichend, aber davon ist eher abzuraten.
- **Note** — freie Annotationen zum Client.

## Wie Klacksy sich verhalten soll

Legt Klacksy einen Client an, erzeugt es mindestens `client` + `address` + `communication` +
`membership`; Email und Telefon aktiv erfragen; auf eine valide Adresse hinwirken; bei
Mitarbeitern einen passenden Contract zuweisen statt stillschweigend auf den Settings-Default zu
fallen.

## Verwandte Skills

- `create_employee` / `update_client` — Stammdaten anlegen/ändern
- `validate_address` — Geocoding-Validierung (openrouteservice)
- `add_client_to_group` / `assign_contract_to_client` — Struktur & Arbeitsbedingungen

## Trigger-Phrasen

- "Wie lege ich in Klacks einen Mitarbeiter/Kunden an?"
- "Was braucht ein Client mindestens?"
- "Warum braucht ein Client eine Email?"
- "Was ist der Unterschied zwischen Workplace- und Rechnungsadresse?"
- "How does Klacks create a client and what is mandatory?"
