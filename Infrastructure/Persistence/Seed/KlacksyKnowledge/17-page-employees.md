---
name: explain_page_employees
description: |
  Explains the "All Addresses" page (/workplace/client) — the central, searchable and
  filterable list of all persons in Klacks: employees, external employees and customers.
  Covers the address table (columns NO/COMPANY/FIRST NAME/LAST NAME, type abbreviation,
  sorting, pagination, Excel export, edit/delete actions), the filter panel (salutation,
  type, validity, countries, cantons/states, visibility date range, deleted addresses)
  and the global header search. Use this when the user asks what they see on the
  Employees/Addresses page, what the cards/columns mean, or how to work with it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - addresses
  - adressen
  - adressverwaltung
  - alle adressen
  - all addresses
  - mitarbeiterliste
  - employee list
  - client list
  - kundenliste
  - personenliste
synonyms:
  de: [alle adressen, adressverwaltung, mitarbeiterliste, personenliste, was sehe ich hier, erkläre diese seite, was bedeutet diese tabelle, wozu dient diese liste]
  en: [all addresses, address management, employee list, client list, what do i see here, explain this page, what does this table mean, how do i use this page]
  fr: [toutes les adresses, gestion des adresses, liste des employés, que vois-je ici, explique cette page, que signifie ce tableau, comment utiliser cette page]
  it: [tutti gli indirizzi, gestione indirizzi, elenco dipendenti, cosa vedo qui, spiega questa pagina, cosa significa questa tabella, come si usa questa pagina]
---

# Seite „Alle Adressen" — Personenliste (/workplace/client)

## Zweck (1 Satz)

Die Seite **Alle Adressen** (de: "Alle Adressen", en: "All Addresses", fr: "Toutes les
adresses", it: "Tutti gli indirizzi") ist die zentrale Liste aller Personen in Klacks —
Mitarbeiter, Externe und Kunden — zum Suchen, Filtern, Anlegen, Bearbeiten, Löschen und
Exportieren. Erreichbar über das Personen-Icon `open-employees` in der linken
Navigation (Tastenkürzel **Alt+5**) oder direkt über die Route `/workplace/client`.

## Aufbau der Seite

### 1. Card **Adressen** (de: "Adressen", en: "Addresses", fr: "Adresses", it: "Indirizzi")

Die grosse Karte links enthält die Adress-Tabelle.

- **Kopfzeile**: Button **+ Adresse erfassen** (de: "+ Adresse erfassen", en: "+ Add
  Address", fr: "Ajouter une adresse", it: "Aggiungi indirizzo") — nur für Admins,
  öffnet die leere Erfassungsmaske `/workplace/edit-address`. Daneben ein
  **Excel-Export-Icon**: ausgegraut, solange keine Zeile angehakt ist; sobald die
  Kopf-Checkbox oder einzelne Zeilen-Checkboxen gesetzt sind, exportiert ein Klick die
  selektierten Adressen als Excel.
- **Untertitel**: zeigt den letzten Bearbeitungsstand der Adressdaten
  („letzter Stand: <Datum>, bearbeitet von <Autor>").
- **Tabelle** (`myAddressTable`) mit folgenden Spalten:
  - **Checkbox** — Zeile für den Excel-Export selektieren; Kopf-Checkbox wählt alle.
  - **Typkürzel** — erster Buchstabe des Personentyps: **Mitarbeiter**
    (de: "Mitarbeiter", en: "Employee", fr: "Employé", it: "Dipendente"), **Externer**
    (de: "Externer", en: "External", fr: "Externe", it: "Esterno") oder **Kunde**
    (de: "Kunde", en: "Customer", fr: "Client", it: "Cliente").
  - **NR** (de: "NR", en: "NO", fr: "NO", it: "NR") — die interne ID-Nummer (`idNumber`).
  - **FIRMA** (de: "FIRMA", en: "COMPANY", fr: "ENTREPRISE", it: "AZIENDA").
  - **VORNAME** (de: "VORNAME", en: "FIRST NAME", fr: "PRÉNOM", it: "NOME").
  - **NACHNAME** (de: "NACHNAME", en: "LAST NAME", fr: "COGNOME", it: "COGNOME").
  - **Aktionen** (rechts): Admins sehen einen **Stift** (öffnet
    `/workplace/edit-address/:id` zum Bearbeiten) und einen **roten Papierkorb**
    (Löschen mit Bestätigungsdialog, Soft-Delete); Nicht-Admins sehen stattdessen ein
    **Auge** (nur ansehen).
- **Sortierung**: Klick auf die Spaltenköpfe NR / FIRMA / VORNAME / NACHNAME sortiert
  auf- bzw. absteigend (Pfeil zeigt die Richtung); Standard ist Nachname aufsteigend.
- **Gelöschte Zeilen**: Ist der Filter „Gelöschte Adressen" aktiv, erscheinen
  soft-gelöschte Einträge optisch markiert und ohne Lösch-Button.
- **Pagination** unten: Seitennavigation plus Auswahl „Zeilen pro Seite"; im
  Automatik-Modus passt sich die Zeilenzahl der Fensterhöhe an.

### 2. Filter-Leiste rechts

Schmale Spalte mit allen Listenfiltern (wirken sofort auf die Tabelle):

- **Anrede-Checkboxen**: **Frau** (de: "Frau", en: "Mrs.", fr: "Madame", it: "Signora"),
  **Herr** (de: "Herr", en: "Mr.", fr: "Monsieur", it: "Signore"), **Neutrale Anrede**
  (de: "Neutrale Anrede", en: "Neutral Address", fr: "Adresse neutre", it: "Saluto
  neutrale"), **Juristische Person** (de: "Juristische Person", en: "Legal Entity",
  fr: "Personne morale", it: "Persona Giuridica"). Mindestens eine muss aktiv sein.
- Dropdown **Typ** (de: "Typ", en: "Type", fr: "Type", it: "Tipo") — filtert nach
  Mitarbeiter / Externer / Kunde.
- Dropdown **Gültigkeit** (de: "Gültigkeit", en: "Validity", fr: "Validité",
  it: "Validità") — Membership-Status: **Aktive** (de: "Aktive", en: "Active",
  fr: "Actifs", it: "Attivi"), **Ehemalige** (de: "Ehemalige", en: "Alumni"),
  **Zukünftige** (de: "Zukünftige", en: "Future", fr: "Futurs", it: "Futuri").
- Dropdown **Nationen auswählen** (de: "Nationen auswählen", en: "Select Countries",
  fr: "Choisir les pays", it: "Seleziona Paesi") — Länderliste mit
  Alle-selektieren/-deselektieren.
- Dropdown **Ktn./Ldr. auswählen** (de: "Ktn./Ldr. auswählen", en: "Select States",
  fr: "Choisir les dép.", it: "Scegliere i dip.") — Kantone bzw. Bundesländer.
- Dropdown **Sichtbarkeit** (de: "Sichtbarkeit", en: "Visibility", fr: "Visibilité",
  it: "Visibilità") — filtert auf **Eintritt** (de: "Eintritt", en: "Entry") und/oder
  **Austritt** (de: "Austritt", en: "Resignation") innerhalb eines Von/Bis-Zeitraums
  (zwei Datumsfelder mit Kalender-Picker).
- Checkbox **Gelöschte Adressen** (de: "Gelöschte Adressen", en: "Deleted Addresses",
  fr: "Adresses supprimées", it: "Indirizzi Eliminati") — blendet soft-gelöschte
  Einträge ein.
- Button **Filter zurücksetzten** (de: "Filter zurücksetzten", en: "Reset Filter",
  fr: "Réinitialiser le filtre", it: "Resetta Filtro") — setzt alle Filter auf Standard.

### 3. Globale Suche im Header

Auf dieser Seite ist das Suchfeld in der Kopfleiste aktiv: ab 2 Zeichen erscheinen
Namens-Vorschläge; eine eingegebene Zahl sucht nach der ID-Nummer; mehrere ID-Nummern
können mit `;` getrennt und mit Enter als Filter angewendet werden.

## Typische Aufgaben (und passende Klacksy-Skills)

- Neue Person/Mitarbeiter/Kunde anlegen → `create_employee`
- Person finden / Liste filtern → `search_employees`, `search_in_list`, `search_and_navigate`
- Details einer Person nachschlagen → `get_client_details`
- Stammdaten ändern → `update_client`, `add_client_email`, `add_client_phone`
- Gruppe oder Vertrag zuweisen → `add_client_to_group_by_name`, `assign_contract_by_name`
- Person löschen → `delete_client`; Adresse geografisch prüfen → `validate_address`

## Verwandte Seiten

- `/workplace/edit-address/:id` — Detailmaske mit Stammdaten/Persona, Adressen,
  Mitgliedschaft, Gruppen, Verträgen, Qualifikationen, Bild und Notiz
  (siehe `explain_address_management`).
- `/workplace/group` — Gruppenverwaltung (Personen strukturieren).
- `/workplace/client-availability` — Verfügbarkeiten der Mitarbeiter.
