---
name: explain_page_import_employees
description: |
  Explains the employee import, where an administrator takes over a whole staff list from an Excel
  (.xlsx) or CSV file instead of creating every person by hand. It lives in the settings card Import
  employees (section Import, expert mode, admins only): the file goes onto the drop zone of the card, then
  a window leads through three steps (mapping with rules for missing information, preview, result).
  Covers the file limits, the downloadable template, the manual, how duplicates, contracts, groups, the
  person type and the personnel number are handled. Use this when the user asks how to import employees
  from Excel or CSV or where to upload a staff list. Supports a level parameter: short (purpose and
  where), elements (every step and control), effects (what is written, duplicates and pitfalls).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - excel import
    - csv import
  de:
    - mitarbeiter importieren
    - mitarbeitende importieren
    - mitarbeiterimport
    - mitarbeiterliste hochladen
  en:
    - import employees
    - employee import
    - upload employee list
synonyms:
  de: [wie importiere ich mitarbeiter aus excel, mitarbeiter importieren, mitarbeiterliste hochladen, wo kann ich eine csv mit mitarbeitern hochladen, excel liste mit mitarbeitenden einlesen, personal aus excel übernehmen, vorlage für den mitarbeiterimport]
  en: [how do i import employees from excel, import employees, upload employee list, where can i upload a csv with employees, import staff from a spreadsheet, template for the employee import]
  fr: [importer des collaborateurs, importer des employés depuis excel, téléverser la liste des collaborateurs, importer un fichier csv de collaborateurs]
  it: [importare collaboratori, importare dipendenti da excel, caricare l'elenco dei collaboratori, importare un file csv di collaboratori]
---

# Employee import — taking over a staff list from Excel or CSV

<!-- level:short -->

## Stage 1 — What is it for and where is it?

The employee import takes over a whole staff list from one Excel file (.xlsx) or one CSV file (.csv)
with one row per person, instead of creating every person by hand. Klacks recognises the columns
itself, shows how every record will look and only then saves. It has no page of its own: it is the
card **Import employees** (de: "Mitarbeitende importieren", en: "Import employees",
fr: "Importer des collaborateurs", it: "Importa collaboratori") in the settings, section **Import**
(de/en: "Import", fr: "Importation", it: "Importazione"). The section Import is only shown in expert
mode: the user first turns on the switch **Expert mode** at the top of the settings page
(de: "Experten Modus", en: "Expert Mode", fr: "Mode expert", it: "Modalità esperto"). When Klacksy
navigates to the card (`navigate_to`, page `settings`, target `client-import`), the settings page
turns expert mode on and opens the section by itself. Only administrators see the card.

The card has two tabs: **Import** (de/en: "Import", fr: "Importation", it: "Importazione")
(`client-import-tab-import`) and **Manual** (de: "Handbuch", en: "Manual", fr: "Manuel",
it: "Manuale") (`client-import-tab-manual`). The tab Import holds the upload zone
(`client-import-drop-zone`): drag the file onto it or click it to choose one. Klacksy cannot upload
a file itself; the user drops or chooses it there. As soon as the file has been read, a window opens
over the settings page and leads through three steps shown at its top (`client-import-steps`):
**Mapping & rules** (de: "Zuordnung & Regeln", en: "Mapping & rules", fr: "Correspondance & règles",
it: "Associazione & regole") → **Preview** (de: "Vorschau", en: "Preview", fr: "Aperçu",
it: "Anteprima") → **Result** (de: "Ergebnis", en: "Result", fr: "Résultat", it: "Risultato").
While the window is open, it covers the rest of the screen, the chat with Klacksy included; Klacksy
can answer again once the window is closed.

The tab Manual (`client-import-manual`) shows the multilingual import handbook (25 languages) with
every column, accepted value, limit and common error message. Inside the window, the button
**Manual** (de: "Handbuch", en: "Manual", fr: "Manuel", it: "Manuale") at the top opens the same
handbook in a second window on top; for details beyond this explanation, point the user there.

<!-- level:elements -->

## Stage 2 — The steps in detail

### On the card — choose the file

- **Upload zone** (`client-import-drop-zone`): drag an .xlsx or .csv file here or click to open the
  file dialog. Limits per file: at most 5 MB, at most 1000 rows and at most 60 columns. The header row
  may sit in one of the first rows; Klacks finds it. The file itself is never stored, only read.
- **Download template** (de: "Vorlage herunterladen", en: "Download template",
  fr: "Télécharger le modèle", it: "Scarica il modello") (`client-import-template-button`): an Excel
  template in the user's language. With the template every column is recognised reliably.
- A file problem (wrong format, too large, too many rows, unreadable) is shown directly under the
  upload zone on the card; the window only opens for a file that could be read.

### Step 1 in the window — Mapping & rules

- **Map columns** (de: "Spalten zuordnen", en: "Map columns", fr: "Associer les colonnes",
  it: "Associa le colonne"): one line per column of the file with sample values, the field Klacks
  suggests and how sure it is. Every suggestion can be changed in the dropdown, or the column can be
  left out with **Do not import** (de: "Nicht übernehmen", en: "Do not import",
  fr: "Ne pas importer", it: "Non importare"). A workbook with several sheets offers a **Sheet**
  (de: "Tabellenblatt", en: "Sheet", fr: "Feuille", it: "Foglio") choice. If dates are ambiguous
  (no day above 12), the user picks the date format; for a single full-name column, the name order.
- Besides names, address, e-mail, phone, birthdate, gender or salutation, entry and exit date and a
  note, the file may also carry a contract column, a group column, a type column (employee, external
  employee or customer) and a company column.
- **Rules for missing information** (de: "Regeln für fehlende Angaben",
  en: "Rules for missing information", fr: "Règles pour les informations manquantes",
  it: "Regole per le informazioni mancanti"): contract and group for rows without one, entry date
  (default today), default country, which kind of e-mail and phone number the columns hold, what
  happens with former employees (skip them or import them with their exit date) and with
  **Possible duplicates** (de: "Mögliche Dubletten", en: "Possible duplicates",
  fr: "Doublons possibles", it: "Possibili duplicati"): skip them (default) or create them anyway.
- The buttons sit at the bottom of the window: **Continue to preview** (de: "Weiter zur Vorschau",
  en: "Continue to preview", fr: "Continuer vers l'aperçu", it: "Continua all'anteprima")
  (`client-import-to-preview`) moves on; **Choose another file** (de: "Andere Datei wählen",
  en: "Choose another file", fr: "Choisir un autre fichier", it: "Scegli un altro file") closes the
  window and goes back to the upload zone of the card.

### Step 2 in the window — Preview

- A summary counts the rows: total, ready, skipped, with errors, duplicates, with warnings. Notes that
  concern (nearly) every row stand once above the table.
- The table shows every row as it will be saved, with its errors first, then warnings, then hints.
  **Show only rows with problems** (de: "Nur Zeilen mit Problemen anzeigen",
  en: "Show only rows with problems", fr: "Afficher uniquement les lignes avec problèmes",
  it: "Mostra solo le righe con problemi") narrows the list. Per row the user can skip it, choose the
  gender when the file has none, or keep a duplicate with **Create anyway** (de: "Trotzdem anlegen",
  en: "Create anyway", fr: "Créer quand même", it: "Crea comunque").
- At the bottom of the window, **Back** (de: "Zurück", en: "Back", fr: "Retour", it: "Indietro")
  returns to the mapping, and the import button (`client-import-commit`) shows how many people will be
  created. It stays disabled while a row still has an error: fix the row (for example by going back to
  the mapping) or skip it.

### Step 3 in the window — Result

Shows how many people were created and how many rows were skipped. Addresses are placed on the map in
the background, which can take a few minutes. At the bottom of the window, **Go to employee list**
(de: "Zur Mitarbeiterliste", en: "Go to employee list", fr: "Vers la liste des collaborateurs",
it: "All'elenco dei collaboratori") (`client-import-to-list`) opens the employee list, and
**Import another file** (de: "Weitere Datei importieren", en: "Import another file",
fr: "Importer un autre fichier", it: "Importa un altro file") closes the window for the next file.

### Closing the window

The × at the top right and the Esc key close the window immediately, without asking. During mapping and
preview nothing has been saved yet, so closing simply discards the import. A click outside the window
does nothing. While the import is being saved the window cannot be closed. After closing, the card shows
the upload zone again.

<!-- level:effects -->

## Stage 3 — What is written and what to watch out for

- **All or nothing**: the import saves every ready row in one go; if anything fails, nothing is saved.
- **Personnel number**: Klacks always assigns it itself. A personnel-number column in the file is
  recognised but ignored.
- **Type and company**: a type column says per row whether it is an employee, an external employee or
  a customer; an empty cell means employee, an unknown word is an error in the preview. A company
  column fills the company name. A customer row with a company is created as a company (legal
  entity), not as a person; a first and last name in that row are optional and kept as the contact
  person (the preview marks them "Contact person"), and its gender cell is ignored. Such a row needs
  an address with postcode, city and country (the default country counts). A customer without a
  company stays a person. Customers never get a contract, neither from the file nor from the rules.
- **Gender is required** for persons: taken from a gender or salutation column; without one, the user
  chooses it per row in the preview. A company needs no gender.
- **Contract and group**: a value in the file is matched by name against the existing contracts and
  groups. An unknown or empty value falls back to the contract or group chosen in the rules, and the
  preview says so. Contracts and groups are not created by the import; set them up first in the
  settings and on the groups page.
- **Duplicates**: the same e-mail address, or the same first name, last name and birthdate, counts as
  a duplicate, both against people already in Klacks and against earlier rows of the same file;
  for a customer with a company, the same company with the same postcode and street.
  Customers are compared only with customers, employees and external employees with each other.
  Such rows are skipped unless the rule or the row says create anyway. The same name alone, or the
  same company at another address, is only a hint.
- **Afterwards**: imported people are ordinary records. Mistakes are corrected or deleted on the page
  All addresses. Importing the same file a second time finds rows with an e-mail address or a
  birthdate again as duplicates; rows with only a name are merely flagged, so check them before
  importing twice.
- **Other ways in**: a single person is created by hand on the employee page; a company directory can
  be synchronised by an administrator through the identity provider settings.
- **Permission**: only administrators see the card and can import. Other users cannot import
  employees and need an administrator for it. An administrator who does not find the card has expert mode
  switched off: turn on the switch at the top of the settings page, then open the section Import.

### Trigger phrases

- "How do I import employees from Excel?"
- "Where can I upload a CSV with employees?"
- "Wie importiere ich Mitarbeiter aus einer Excel-Liste?"
- "Gibt es eine Vorlage für den Mitarbeiterimport?"
