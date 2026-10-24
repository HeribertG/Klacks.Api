---
name: explain_page_employees
description: |
  Explains the All Addresses page (Alle Adressen) at /workplace/client — the central,
  searchable and filterable list of all persons in Klacks (employees, external employees,
  customers) — and the edit mask at /workplace/edit-address/:id. Covers the address table
  (columns NR/COMPANY/FIRST NAME/LAST NAME, type abbreviation, sorting, pagination, Excel
  export, edit/delete/view actions), the filter panel (salutation, type, validity/membership
  status, countries, cantons, visibility date range, deleted addresses, reset), the global
  header search (name or ID number, multiple IDs separated by ';', include-address option)
  and the edit mask cards: persona/master data (company, salutation, first/last name,
  birthdate), historized addresses (a move adds a NEW address with valid-from instead of
  overwriting; types main/company/invoicing address), communication (phone/email),
  membership (entry date = plannability limit in the schedule), contracts, assigned groups,
  qualifications, notes and image. Use this when the user asks what they see on the
  Employees/Addresses page or in the address edit mask, what the columns/cards mean, or how
  to work with them. Supports a level parameter: short (purpose only), elements (every
  element explained), effects (data sources and how the page interacts with schedule,
  absence calendar, groups, availability and the planning assistant).
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
  - adresse bearbeiten
  - edit address
  - stammdaten
  - master data
  - adresshistorie
  - address history
  - historisierung
  - neue adresse
  - new address
  - mitgliedschaft
  - membership
  - eintrittsdatum
  - entry date
  - austrittsdatum
  - rechnungsadresse
  - hauptadresse
  - geschäftsadresse
  - excel export
  - gelöschte adressen
  - deleted addresses
synonyms:
  de: [alle adressen, adressverwaltung, mitarbeiterliste, personenliste, was sehe ich hier, erkläre diese seite, was bedeutet diese tabelle, wozu dient diese liste, adresse bearbeiten, stammdaten ändern, neue adresse erfassen, adresshistorie, alte adresse, umzug erfassen, eintrittsdatum, austrittsdatum, gelöschte adressen anzeigen, excel export der adressen, nach id-nummer suchen, liste sortieren]
  en: [all addresses, address management, employee list, client list, what do i see here, explain this page, what does this table mean, how do i use this page, edit address, change master data, add a new address, address history, old address, record a move, entry date, exit date, show deleted addresses, export addresses to excel, search by id number, sort the list]
  fr: [toutes les adresses, gestion des adresses, liste des employés, que vois-je ici, explique cette page, que signifie ce tableau, comment utiliser cette page, modifier l'adresse, données de base, nouvelle adresse, historique des adresses, date d'entrée, date de sortie, adresses supprimées, export excel, trier la liste]
  it: [tutti gli indirizzi, gestione indirizzi, elenco dipendenti, cosa vedo qui, spiega questa pagina, cosa significa questa tabella, come si usa questa pagina, modificare indirizzo, dati anagrafici, nuovo indirizzo, storico indirizzi, data di ingresso, data di uscita, indirizzi eliminati, esportazione excel, ordinare la lista]
---

# Adressverwaltung — die Seiten /workplace/client und /workplace/edit-address/:id

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Die Seite **Alle Adressen** (de: "Alle Adressen", en: "All Addresses", fr: "Toutes les
adresses", it: "Tutti gli indirizzi") ist die zentrale Liste aller Personen in Klacks —
Mitarbeiter, Externe und Kunden — zum Suchen, Filtern, Anlegen, Bearbeiten, Löschen und
Exportieren. Erreichbar über das Personen-Icon `open-employees` in der linken Navigation
(Tastenkürzel **Alt+5**) oder die Route `/workplace/client`. Der Stift (bzw. das Auge für
Konten ohne Bearbeitungsrecht) in einer Zeile öffnet die Bearbeitungsmaske `/workplace/edit-address/:id`
(de: "Adresse und persönliche Daten", en: "Address and personal details", fr: "Adresse et
coordonnées personnelles", it: "Indirizzo e dati personali") — dort sind alle Stammdaten
einer Person gebündelt: Persona mit historisierten Adressen, Kommunikation, Mitgliedschaft,
Verträge, Gruppen, Qualifikationen, Notizen und Bild.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand, wirkt auf diese Seite)

- **Suche** (Suchfeld in der Kopfleiste): filtert auf der Listen-Seite die Adress-Tabelle.
  Text sucht in **Vorname, Zweitname, Nachname, Ledigname und Firma** (Teilwort genügt,
  Gross-/Kleinschreibung egal). Suchregeln, die Klacksy dem Nutzer bei Bedarf erklärt:
  - Mehrere Wörter (getrennt durch Leerzeichen, Komma, Punkt, Bindestrich oder `/`) müssen
    **alle** vorkommen, Reihenfolge egal: `Hans Müller` findet dasselbe wie `Müller, Hans`.
  - Text in **Anführungszeichen** bleibt eine zusammenhängende Phrase — für Namen mit Partikel:
    `"von Oberhausen"`, `"de Caprio"`, `"van Felden"`; kombinierbar: `Hans "von Oberhausen"`.
  - Mit `+` verbundene Begriffe sind **ODER**-verknüpft: `Müller+Meier` findet alle Müller UND
    alle Meier. Vor- und Nachname daher nie mit `+` verbinden (`Hans+Müller` findet alle Hans
    und alle Müller).
  - Ein **einzelnes Zeichen** findet Namen, die damit beginnen.
  - Eine **Zahl** sucht nach der ID-Nummer; **mehrere ID-Nummern** mit `;` getrennt (`12;15;20`).
  - Findet die Suche gar nichts, sucht Klacks automatisch nach ähnlich geschriebenen oder
    klingenden Namen (`Mayer` findet `Meier`); Umlaute werden sonst nicht gleichgesetzt.

  Nur auf dieser Seite
  erscheint zusätzlich die Checkbox **inklusive Adresseanangabe** (`search-include-address`,
  de: "inklusive Adresseanangabe", en: "Include address information", fr: "Inclure
  l'adresse", it: "incluso l'indirizzo") — damit durchsucht der Text auch die Adressfelder,
  und eine Zahl findet zusätzlich Telefonnummern und PLZ. Auf der Bearbeitungsmaske ist das
  Suchfeld ausgeblendet.
- **Gruppen-Auswahl** (Dropdown mit Gruppenbaum, Option **Alle Gruppen** (de: "Alle Gruppen",
  en: "All groups", fr: "Tous les groupes", it: "Tutti i gruppi")): begrenzt die Liste auf
  Personen, die der gewählten Gruppe zugeordnet sind. Die Wahl ist global und gilt
  gleichzeitig für alle Arbeitsbereich-Seiten (Schichtplan, Absenzen, Verfügbarkeit usw.).
  Auf die Bearbeitungsmaske selbst hat die Gruppen-Auswahl keine Wirkung.

### Seite „Alle Adressen" — Card **Adressen** (de: "Adressen", en: "Addresses", fr: "Adresses", it: "Indirizzi")

- **Kopfzeile**: Button **+ Adresse erfassen** (`new-address-button`, de: "+ Adresse
  erfassen", en: "+ Add Address", fr: "Ajouter une adresse", it: "Aggiungi indirizzo") —
  nur sichtbar mit dem Recht, Personen anzulegen (Admin und Supervisor), öffnet die leere Erfassungsmaske `/workplace/edit-address`.
  Daneben ein **Excel-Export-Icon**: ausgegraut, solange keine Zeile angehakt ist; sobald
  die Kopf-Checkbox oder einzelne Zeilen-Checkboxen gesetzt sind, exportiert ein Klick die
  selektierten Adressen als Excel.
- **Untertitel**: zeigt den letzten Bearbeitungsstand der Adressdaten (Datum + Autor).
- **Tabelle** (`myAddressTable`) mit folgenden Spalten:
  - **Checkbox** — Zeile für den Excel-Export selektieren; die Kopf-Checkbox wählt alle
    (teilselektiert = indeterminate).
  - **Typkürzel** — erster Buchstabe des Personentyps: **Mitarbeiter** (de: "Mitarbeiter",
    en: "Employee", fr: "Employé", it: "Dipendente"), **Externer** (de: "Externer", en:
    "External", fr: "Externe", it: "Esterno") oder **Kunde** (de: "Kunde", en: "Customer",
    fr: "Client", it: "Cliente").
  - **NR** (de: "NR", en: "NO", fr: "NO", it: "NR") — die interne ID-Nummer (`idNumber`).
  - **FIRMA** (de: "FIRMA", en: "COMPANY", fr: "ENTREPRISE", it: "AZIENDA").
  - **VORNAME** (de: "VORNAME", en: "FIRST NAME", fr: "PRÉNOM", it: "NOME").
  - **NACHNAME** (de: "NACHNAME", en: "LAST NAME", fr: "NOM", it: "COGNOME").
  - **Aktionen** (rechts): Wer Adressen bearbeiten darf (Admin und Supervisor), sieht einen
    **Stift** (`client-edit-button-{i}`, öffnet `/workplace/edit-address/:id`); den **roten
    Papierkorb** (`client-delete-button-{i}`, Löschen mit Bestätigungsdialog) sieht nur, wer
    Adressen löschen darf (nur Admin) und nur bei nicht gelöschten Einträgen. Das Löschen ist
    ein Soft-Delete: die Person verschwindet aus der normalen Liste und bleibt nur über den
    Filter **Gelöschte Adressen** einsehbar; eine Wiederherstellen-Funktion gibt es in der App
    nicht. Wer Adressen nicht bearbeiten darf (Konto ohne Rolle), sieht stattdessen ein
    **Auge** (`client-view-button-{i}`, öffnet die Maske nur zum Ansehen).
- **Sortierung**: Klick auf die Spaltenköpfe NR / FIRMA / VORNAME / NACHNAME sortiert auf-
  bzw. absteigend (Sortierfelder `idNumber`/`company`/`firstName`/`name`, Pfeil zeigt die
  Richtung); Standard ist Nachname aufsteigend.
- **Zeilen**: `client-row-{i}` mit `client-firstname-{i}` / `client-lastname-{i}`; Klick
  auf eine Zeile markiert sie. Gelöschte Einträge (sichtbar nur mit dem Filter „Gelöschte
  Adressen") erscheinen optisch markiert und ohne Lösch-Button.
- **Pagination** unten (`app-pagination`): Seitennavigation plus „Zeilen pro Seite"; eine
  Resize-Direktive passt die Zeilenzahl automatisch an die Fensterhöhe an.

### Filter-Leiste rechts (wirkt sofort auf die Tabelle)

- **Anrede-Checkboxen** (`filter-female`, `filter-male`, `filter-intersexuality`,
  `filter-legal-entity`): **Frau** (de: "Frau", en: "Mrs.", fr: "Madame", it: "Signora"),
  **Herr** (de: "Herr", en: "Mr.", fr: "Monsieur", it: "Signore"), **Neutrale Anrede**
  (de: "Neutrale Anrede", en: "Neutral Address", fr: "Adresse neutre", it: "Saluto
  neutrale"), **Juristische Person** (de: "Juristische Person", en: "Legal Entity", fr:
  "Personne morale", it: "Persona Giuridica").
- Dropdown **Typ** (`dropdownForm-0`, de: "Typ", en: "Type", fr: "Type", it: "Tipo") —
  Checkboxen `filter-type-employee` / `filter-type-extern-emp` / `filter-type-customer`
  für Mitarbeiter / Externer / Kunde; `close-type-dropdown` schliesst das Menü.
- Dropdown **Gültigkeit** (`dropdownForm-1`, de: "Gültigkeit", en: "Validity", fr:
  "Validité", it: "Validità") — Membership-Status relativ zu heute: **Aktive**
  (`filter-active-membership`, de: "Aktive", en: "Active", fr: "Actifs", it: "Attivi"),
  **Ehemalige** (`filter-former-membership`, de: "Ehemalige", en: "Alumni", fr: "Anciens
  élèves", it: "Ex-allievi"), **Zukünftige** (`filter-future-membership`, de: "Zukünftige",
  en: "Future", fr: "Futurs", it: "Futuri").
- Dropdown **Nationen auswählen** (`dropdownForm-2`, de: "Nationen  auswählen", en:
  "Select Countries", fr: "Choisir les pays", it: "Seleziona Paesi") — Länderliste mit
  Alle-selektieren/-deselektieren (`filter-select-all-countries` /
  `filter-deselect-all-countries`).
- Dropdown **Ktn./Ldr. auswählen** (`dropdownForm1`, de: "Ktn./Ldr.  auswählen", en:
  "Select States", fr: "Choisir les dép.", it: "Scegliere i dip.") — Kantone bzw.
  Bundesländer der gewählten Länder, ebenfalls mit Alle-selektieren/-deselektieren.
- Dropdown **Sichtbarkeit** (`dropdownForm4`, de: "Sichtbarkeit", en: "Visibility", fr:
  "Visibilité", it: "Visibilità") — filtert auf **Eintritt** (`scopeFromFlag`, de:
  "Eintritt", en: "Entry", fr: "Entrée", it: "Ingresso") und/oder **Austritt**
  (`scopeUntilFlag`, de: "Austritt", en: "Resignation", fr: "Départ", it: "Dimissioni")
  innerhalb eines **Von/Bis**-Zeitraums (Datums-Picker `filter-date-from` /
  `filter-date-until`); verglichen werden die Mitgliedschaftsdaten (Eintritts-/
  Austrittsdatum).
- Checkbox **Gelöschte Adressen** (`filter-show-deleted`, de: "Gelöschte Adressen", en:
  "Deleted Addresses", fr: "Adresses supprimées", it: "Indirizzi Eliminati") — zeigt
  AUSSCHLIESSLICH die gelöschten Einträge (nicht zusätzlich zu den aktiven).
- Button **Filter zurücksetzten** (`filter-reset-button`, de: "Filter zurücksetzten", en:
  "Reset Filter", fr: "Réinitialiser le filtre", it: "Resetta Filtro") — setzt alle Filter
  auf Standard.

### Bearbeitungsmaske „Adresse und persönliche Daten" (/workplace/edit-address/:id)

Ohne `:id` (Route `/workplace/edit-address`) wird eine neue Person erfasst; mit
Query-Parameter `readonly=true` ist die Maske schreibgeschützt (Ansicht für Nicht-Admins).
Container: `edit-address-home-container`. Die Maske besteht aus untereinander liegenden
Cards plus einer rechten Navigationsspalte:

- **Persona-Card** (Titel: **ID Nummer** (de: "ID Nummer:", en: "ID Number:", fr: "Numéro
  d'identification:", it: "Numero ID:") + Typkürzel + Nummer). Linke Spalte — Stammdaten
  und Adresse:
  - **Firma** (`company`, de: "Firma", en: "Company", fr: "Société", it: "Azienda") — nur
    bei juristischen Personen.
  - **Anrede** (`gender`, de: "Anrede", en: "Salutation", fr: "Salutation", it: "Titolo")
    mit Herr/Frau/Neutrale Anrede; Checkbox **Juristische Person** (`legalEntity`).
  - **Titel** (`title`), **Vorname** (`firstname`, per `+` zweite Vornamenszeile),
    **Nachname** (`profile-name`, per `+` Ledigname).
  - **Strasse** (`street`, de: "Strasse", en: "Street", fr: "Rue", it: "Via"; per `+` bis
    zu drei Zeilen `street2`/`street3`), **PLZ** (`zip` — beim Verlassen wird der Ort
    automatisch vorgeschlagen), **Ort** (`city` mit Vorschlagsliste), **Kanton/Bundesland**
    (`state`, nur wenn das Land Regionen hat), **Nation** (`country`, de: "Nation", en:
    "Country", fr: "Pays", it: "Nazione").
  - Pflichtfelder sind mit `*` markiert und werden live rot/grün validiert.
  - Rechte Spalte — Kommunikation und Geburtsdatum: **Telefon**-Zeilen (Vorwahl-Dropdown +
    Nummer + Typ; `add-phone-button` fügt weitere hinzu, `-` entfernt), **E-Mail**-Zeilen
    (Wert + Typ; `add-email-button`), **Geburtsdatum** (`profile-birthday`, Datums-Picker).
  - **Header-Aktionen**: bei Kunden mit Koordinaten ein OpenStreetMap- und ein
    Street-View-Button; das Options-Menü (`address-persona-options-button`) enthält
    **Neue Adresse erstellen** (`new-address-link`, de: "Neue Adresse erstellen", en:
    "Create new address", fr: "Créer une nouvelle adresse", it: "Crea un nuovo indirizzo")
    und **Adresse deaktivieren** (`delete-address-link`, de: "Adresse deaktivieren", en:
    "Deactivate Address", fr: "Désactiver l'adresse", it: "Disattiva indirizzo").
  - **Adress-Historisierung**: Bei einem Umzug wird die bestehende Adresse NICHT
    überschrieben — über **Neue Adresse erstellen** öffnet sich das Modal **Neue Adresse**
    (de: "Neue Adresse", en: "New Address") mit **Adresse gültig ab** (Datums-Picker
    `newAddressValidFrom`) und **Adressart** (`newAddressType`): **Hauptadresse**,
    **Geschäftsadresse** oder **Rechnungsadresse** — je nach Typ der Person: Mitarbeiter
    haben nur die Hauptadresse (das Feld Adressart wird dann nicht angezeigt), Externe
    Hauptadresse und Geschäftsadresse, Kunden alle drei. Der Typ lässt sich nicht
    wechseln, solange Adressen einer nicht mehr erlaubten Art existieren. Die neue Adresse
    wird zusätzlich angehängt; die alte bleibt als Historie erhalten.
- **Registration-Card** (`membership-form`, de: "Registration", en: "Registration", fr:
  "Inscription", it: "Registrazione") — die Mitgliedschaft:
  - **Typ** (`client-type`, Dropdown Mitarbeiter / Externer / Kunde).
  - **Eintrittsdatum** (`membership-entry-date`, Pflichtfeld, de: "Eintrittsdatum", en:
    "Entry Date", fr: "Date d'entrée", it: "Data di ingresso") — das ist die
    **Planbarkeitsgrenze im Schichtplan**.
  - **Austrittsdatum** (`membership-until-date`, optional, de: "Austrittsdatum", en:
    "Exit Date", fr: "Date de sortie", it: "Data di uscita").
- **Verträge-Card** (de: "Verträge", en: "Contracts", fr: "Contrats", it: "Contratti") —
  nur sichtbar, wenn der Typ NICHT Kunde ist; Vertragszuordnungen der Person.
- **Zugewiesene Gruppen-Card** (de: "Zugewiesene Gruppen", en: "Assigned Groups", fr:
  "Groupes assignés", it: "Gruppi assegnati") — nur sichtbar, wenn Root-Gruppen existieren;
  Gruppenzuordnungen der Person.
- **Qualifikationen-Card** (de: "Qualifikationen", en: "Qualifications", fr:
  "Qualifications", it: "Qualifiche") — nur sichtbar, wenn der Typ NICHT Kunde ist.
- **Messenger-Kontakte-Card** — nur sichtbar, wenn das Messaging-Plugin aktiviert ist.
- **Notizen-Card** (de: "Notizen", en: "Notes", fr: "Notes", it: "Note") und **Bild-Card**
  (de: "Bild", en: "Image", fr: "Image", it: "Immagine") — Anmerkungen und Profilbild.
- **Rechte Spalte** (Adress-Navigation):
  - **Aktuelle Adressen** (de: "Aktuelle Adressen", en: "Current Addresses", fr: "Adresses
    actuelles", it: "Indirizzi attuali"), **Zukünftige Adressen** (de: "Zuküftige
    Adressen", en: "Future addresses", fr: "Adresses futures", it: "Indirizzi futuri") und
    **Vergangene Adressen** (de: "Vergangene Adressen", en: "Past Addresses", fr:
    "Adresses passées", it: "Indirizzi passati") — pro Adresse ein Button
    (`active-address-{i}` / `future-address-{i}` / `past-address-{i}`) mit der Adressart
    als Beschriftung und Tooltip **Gültig ab** (de: "Gültig ab:", en: "Valid from:", fr:
    "Valide à partir de :", it: "Valido da:") + Datum; Klick wechselt die in der
    Persona-Card angezeigte Adresse (so ist die Adress-Historie einsehbar).
  - Bei der Neuerfassung erscheint die Liste **Gefunden** (de: "Gefunden", en: "Found",
    fr: "Trouvé", it: "Trovato"): während des Tippens werden ähnliche bestehende Personen
    angezeigt (`find-client-{i}`, mit eigener Mini-Pagination) — ein Dubletten-Check; ein
    Klick springt zur bestehenden Person, `reset-address-button` (de: "Zurück zur neuen
    Adr.", en: "Back to new Addr.") kehrt zur Neuerfassung zurück.
- **Untere Speicherleiste** (global eingeblendet): Link **Zurück** (de: "Zurück", en:
  "Back", fr: "Retour", it: "Indietro"), bei ungespeicherten Änderungen **Zurücksetzen**
  (de: "Zurücksetzen", en: "Reset", fr: "Réinitialiser", it: "Reset") und der
  Speichern-Button (`shift-save-btn`, de: "Eingaben speichern", en: "Save entries", fr:
  "Enregistrer les entrées", it: "Salva le voci").
- **Adress-Validierung beim Speichern**: Die Adresse wird gegen den Geocoding-Dienst
  geprüft. Schlägt die Prüfung fehl, erscheint ein interaktiver Hinweis mit
  Korrektur-Vorschlägen und der Option **Trotzdem speichern** (de: "Trotzdem speichern",
  en: "Save anyway", fr: "Enregistrer quand même", it: "Salva comunque"); ein gewählter
  Vorschlag übernimmt Strasse/PLZ/Ort samt Koordinaten.

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenbasis**: Die Liste lädt gefilterte, paginierte Clients über die Client-Filter-Logik
  des Backends; dabei greifen nacheinander Gruppen-Filter, Suche, Anrede-/Typ-Filter,
  Membership-Status-Filter (Aktive/Ehemalige/Zukünftige relativ zu heute),
  Sichtbarkeits-Zeitraum (auf Ein-/Austrittsdatum der Mitgliedschaft) und Sortierung.
- **Gelöschte Adressen**: Gelöschte Personen sind standardmässig ausgeschlossen. Der Filter
  „Gelöschte Adressen" schaltet die Liste komplett auf NUR gelöschte Einträge um; dort gibt
  es keinen Lösch-Button mehr. Löschen ist ein Soft-Delete: die Person wird nicht endgültig
  entfernt und bleibt über diesen Filter einsehbar, kann in der App aber nicht
  wiederhergestellt werden.
- **Gruppen-Scope und Sichtbarkeit**: Die globale Gruppen-Auswahl wirkt seitenübergreifend —
  wer hier die Gruppe wechselt, sieht auch im Schichtplan und in der Verfügbarkeit nur noch
  diese Gruppe (und umgekehrt). Zusätzlich beschränkt das Backend Nicht-Admins immer auf
  die Personen ihrer sichtbaren Gruppen — „fehlende" Personen sind oft nur durch
  Gruppen-Auswahl, Filter oder Suche ausgeblendet.
- **Mitgliedschaft → Einsatzplanung (`/workplace/schedule`)**: Das **Eintrittsdatum** der
  Mitgliedschaft ist die Planbarkeitsgrenze im Schichtplan — eine Person ist erst ab diesem
  Datum planbar (massgeblich ist die Mitgliedschaft, NICHT das Vertragsdatum; eine Person
  kann mehrere Verträge haben). Der Absenzen-Kalender validiert Einträge ebenfalls gegen
  die Mitgliedschaftsperiode.
- **Typ steuert die Maske und die Planung**: Kunden haben keine Verträge- und
  Qualifikationen-Cards. Klacks erstellt Planungen für Mitarbeiter und Externe im Auftrag
  von Kunden.
- **Adress-Historisierung**: Da Umzüge als neue Adresse mit „gültig ab" erfasst werden,
  bleibt nachvollziehbar, welche Adresse zu welchem Zeitpunkt galt (aktuelle, zukünftige
  und vergangene Adressen in der rechten Spalte). Die Adressarten Hauptadresse /
  Geschäftsadresse / Rechnungsadresse erlauben getrennte Anschriften pro Zweck; welche
  Arten möglich sind, hängt vom Typ ab (Mitarbeiter: Hauptadresse; Extern: zusätzlich
  Geschäftsadresse; Kunde: zusätzlich Rechnungsadresse).
- **Geocoding**: Beim Speichern validierte Adressen erhalten Koordinaten — Grundlage für
  die Karten-Buttons (OpenStreetMap/Street View) bei Kunden und für geografische
  Funktionen.
- **Szenarien & Periodenabschluss**: Stammdaten sind davon nicht betroffen —
  Szenario-Isolation und Periodenabschluss/Tagessperre wirken auf Planungsdaten
  (Dienste/Absenzen), nicht auf Adressen.
- **Berechtigungen**: Anlegen und Bearbeiten dürfen Admin und Supervisor, Löschen nur der Admin; Konten ohne Rolle
  öffnen die Maske nur lesend (Auge / `readonly=true`).
- **Dubletten-Schutz**: Bei der Neuerfassung sucht Klacks live nach ähnlichen bestehenden
  Personen und zeigt sie in der „Gefunden"-Liste, bevor versehentlich ein Duplikat
  entsteht.

### Typische Aufgaben

- Die Edit-Maske ansehen, während sie erklärt wird — Skill `navigate_to`
  (Ziel "new-employee" öffnet die leere Adress-Maske)

- Neue Person/Mitarbeiter/Kunde anlegen — Skill `create_employee`
- Liste öffnen und filtern ("zeig mir alle Kunden") — Skill `search_in_list`
  (entityType "client", optional entityTypeFilter customer/employee/extern,
  searchQuery = Name)
- Liste auf eine Gruppe eingrenzen oder wieder alle zeigen — Skill `select_group`
  (Gruppen-Name oder "all")
- Person per Kriterien in der Datenbank suchen — Skill `search_employees`; direkt zur
  Bearbeitungsmaske springen — Skill `search_and_navigate`
- Details einer Person nachschlagen — Skill `get_client_details`
- Stammdaten ändern — Skills `update_client`, `update_client_birthdate`,
  `update_client_gender`
- Umzug erfassen (Historisierung: NEUE Adresse mit "gültig ab", alte bleibt erhalten) —
  Skill `create_address` (Parameter `validFrom`); eine bestehende Adresse korrigieren —
  Skill `update_address`; Adresse deaktivieren — Skill `delete_address`
- Kommunikation ergänzen — Skills `add_client_email`, `add_client_phone`; Notiz — Skill
  `add_client_note`
- Kommunikation oder Notizen ändern/entfernen — Skills `update_communication` /
  `delete_communication`, `update_annotation` / `delete_annotation`
- Austritt erfassen (Austrittsdatum der aktiven Mitgliedschaft setzen; ab dem Folgetag
  ist die Person nicht mehr planbar) — Skill `end_client_membership`; Mitgliedschaft
  gezielt anpassen oder entfernen — Skills `update_membership` / `delete_membership`
- Gruppe oder Vertrag zuweisen — Skills `add_client_to_group_by_name` /
  `remove_client_from_group`, `assign_contract_by_name`
- Qualifikation setzen — Skill `set_client_qualification`; Mitgliedschaften einsehen —
  Skill `list_client_memberships`
- Person löschen — Skill `delete_client`; Adresse geografisch prüfen — Skill
  `validate_address`
- Zur Seite springen — Skill `navigate_to` (Ziel "addresses")

### Verwandte Seiten

- Domänen-Wissen zum Anlegen und Vernetzen von Clients (Minimum Client + Adresse +
  Kommunikation + Mitgliedschaft, Client-Typen, Geocoding) — Skill
  `explain_address_management`.
- `/workplace/group` — Gruppenverwaltung (Personen strukturieren, Sichtbarkeit).
- `/workplace/client-availability` — Verfügbarkeiten der Mitarbeiter.
- `/workplace/schedule` — Einsatzplanung; plant Personen ab dem Eintrittsdatum ihrer
  Mitgliedschaft.
- `/workplace/absence` — Absenzen Kalender; Rechtsklick "Adresse anzeigen" springt hierher.

### Trigger-Phrasen

- "Was sehe ich auf der Adressen-Seite?"
- "Wie erfasse ich einen neuen Mitarbeiter?"
- "Was bedeutet das Typkürzel in der Tabelle?"
- "Wie ändere ich die Adresse, wenn jemand umzieht?" (Historisierung — neue Adresse statt
  überschreiben)
- "Erfasse den Austritt von Anna Muster per Ende Monat." (Austrittsdatum der
  Mitgliedschaft — `end_client_membership`)
- "Warum kann ich diese Person nicht im Schichtplan einplanen?" (Eintrittsdatum der
  Mitgliedschaft)
- "How do I export selected addresses to Excel?"
- "How do I show deleted addresses?"

<!-- level:pitfalls -->

## Stufe 4 — Folgen, Endgültigkeit, Grenzfälle

### Person fehlt in der Liste

Die Liste startet mit dem Filter (de: "Gültigkeit", en: "Validity") nur auf (de: "Aktive", en: "Active"). Wer erst in der Zukunft eintritt, steht nur unter (de: "Zukünftige", en: "Future"); wer ausgetreten ist, nur unter (de: "Ehemalige", en: "Alumni"). Gelöschte Personen erscheinen nur mit (de: "Gelöschte Adressen", en: "Deleted Addresses") — und dann ausschliesslich sie. Eine neu erfasste Person mit Eintritt in der Zukunft ist also nach dem Speichern nicht sofort in der Standardliste zu sehen. Dazu kommen Gruppen-Auswahl, Suche und bei Nicht-Admins die Gruppen-Sichtbarkeit.

### Rechte

Admin darf alles. Supervisor darf Personen ansehen, anlegen und bearbeiten, aber nicht löschen. Ein Konto ohne Rolle sieht das Auge statt des Stifts, darf in der Maske nur Notizen schreiben und Stammdaten nicht ändern.

### Löschen

Löschen darf nur der Admin. Es ist ein Soft-Delete: die Person verschwindet aus der Standardliste, bleibt in der Datenbank und ist nur über (de: "Gelöschte Adressen", en: "Deleted Addresses") sichtbar; dort öffnet sie sich schreibgeschützt (alle Cards gesperrt). Eine Wiederherstellen-Funktion gibt es in der App nicht — das Löschen ist aus Sicht der App endgültig. Wenn jemand nur das Unternehmen verlässt, ist das Austrittsdatum der richtige Weg, denn es lässt sich ändern oder wieder leeren.

### Typ ändern

Das Dropdown (de: "Typ", en: "Type") in der Card (de: "Registration", en: "Registration") übernimmt den neuen Wert ohne Prüfung; erst das Speichern wird abgelehnt, wenn die Person noch eine Adressart hat, die der neue Typ nicht erlaubt (etwa eine Geschäftsadresse bei einem Mitarbeiter). Diese Adressen zuerst entfernen, dann erneut speichern. Eine juristische Person darf nur Kunde sein. Bei Typ Kunde verschwinden die Cards (de: "Verträge", en: "Contracts") und (de: "Qualifikationen", en: "Qualifications"); was mit bereits gespeicherten Verträgen passiert, ist nicht geprüft.

### Adresse: Tippfehler, Umzug, Entfernen

- Ein Tippfehler wird direkt in der angezeigten Adresse korrigiert und ändert damit auch, was vergangene Tage zeigen. Ein echter Umzug ist eine neue Version über (de: "Neue Adresse erstellen", en: "Create new address") mit (de: "Adresse gültig ab", en: "Address valid from"); liegt der Tag in der Zukunft, ist es ein vorgemerkter Umzug und die Version steht bis dahin unter den zukünftigen Adressen.
- Die „aktuelle" Adresse wird in der Maske je Adressart bestimmt: die neueste Version dieser Art mit Beginn bis heute.
- (de: "Adresse deaktivieren", en: "Deactivate Address") setzt kein Enddatum, sondern nimmt die Version aus der Person heraus; geschrieben wird das erst beim Speichern. Davor verwirft (de: "Zurücksetzen", en: "Reset") in der unteren Leiste alle offenen Änderungen der Maske.

### Geocoding-Ausfall

- Geprüft wird beim Speichern, und nur Adressen mit PLZ und Ort, die sich seit dem letzten Speichern geändert haben. Eine unveränderte Adresse erneut zu speichern holt fehlende Koordinaten nicht nach.
- Findet der Dienst die Adresse nicht, trifft die Strasse nicht genau oder passt der Kanton nicht, bricht das Speichern ab; der Hinweis bietet Vorschläge und (de: "Trotzdem speichern", en: "Save anyway"). Ein gewählter Vorschlag übernimmt Strasse, PLZ und Ort samt Koordinaten; „Trotzdem speichern" überspringt die Prüfung für alle Adressen dieses Speicherns und speichert die eingegebenen Werte, normalerweise ohne Koordinaten.
- Ist der Geocoding-Dienst nicht erreichbar oder abgeschaltet, wird ohne Hinweis ohne Koordinaten gespeichert.
- Ohne Koordinaten fehlen die Karten-Buttons (nur bei Kunden mit Koordinaten und aktivierten externen Kartenlinks), der Skill „nächste Gruppe" ändert nichts und meldet, dass keine Adresse mit Koordinaten vorliegt, und die Standort-Gruppierung kann Adressen ausserhalb ihrer Cluster-Orte nicht einordnen.

### Austritt

- Das Austrittsdatum ist der letzte Tag der Zugehörigkeit; ab dem Folgetag wird die Planung abgelehnt (Schichtplan: auch ausserhalb der Gruppenzeit der Person). Absenzen müssen vollständig innerhalb der Mitgliedschaft liegen.
- Das Soll einer Zahlperiode wird nach Mitgliedschaftstagen anteilig gekürzt (Austritt am 15.03. ergibt 15/31 des Monatssolls); die Maximalstunden werden nie gekürzt.
- Bereits geplante Einträge nach einem neu gesetzten Austrittsdatum bleiben bestehen: Klacks löscht und markiert sie nicht, und beim Speichern des Austrittsdatums kommt keine Warnung. Im Schichtplan sind diese Tage in der Zeile der Person gesperrt; in einer Periode ganz nach dem Austritt wird die Zeile nicht mehr gelistet, die Einträge existieren aber weiter. Deshalb vor dem Setzen des Austrittsdatums die geplanten Einträge der Person prüfen.

### Verträge in der Maske

Hat die Person Vertragszeilen, muss mindestens eine (de: "AKTIV", en: "ACTIVE") sein, sonst lässt sich nicht speichern; die Datumsauswahl bietet keinen Beginn vor dem Eintrittsdatum an. Das Soll richtet sich nach dem aktiven Vertrag am ersten Tag der Zahlperiode — Details in `explain_contracts`.
