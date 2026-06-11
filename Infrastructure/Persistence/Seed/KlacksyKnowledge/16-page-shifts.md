---
name: explain_page_shifts
description: |
  Explains the Shifts page (Dienste / All Services) at /workplace/shift: the service list card
  with its filter views Orders (Bestellungen), Plannable Services (with Time Range and Sporadic
  sub-filters), Container and Absence, the table columns (abbreviation, name, description,
  validity dates, times, weekday/holiday checkboxes), the row action icons (edit pencil, delete,
  scissors for shift cutting, box for container template), the validity filter
  (active/former/future), the sealed-orders checkbox, and the sub-pages edit-shift,
  cut-shift (shift cutting) and container-template. Use this when the user asks what they see
  on the Shifts page, what the cards/columns/icons mean, or how to work with it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - shifts
  - dienste
  - dienstliste
  - shift list
  - bestellungen
  - planbare dienste
  - schichtvorlage
  - zuschnitt
  - services page
  - alle dienste
synonyms:
  de: [dienste-seite, alle dienste, was sehe ich hier, erkläre diese seite, was bedeutet diese karte, was bedeutet diese tabelle, dienstliste, schichtliste]
  en: [services page, all services, what do i see here, explain this page, what does this card mean, what does this table mean, shift list, shifts overview]
  fr: [page services, tous les services, qu'est-ce que je vois ici, explique cette page, que signifie cette carte, liste des services]
  it: [pagina servizi, tutti i servizi, cosa vedo qui, spiega questa pagina, cosa significa questa scheda, elenco dei servizi]
---

# Dienste-Seite (/workplace/shift) — Dienste & Schichtvorlagen verwalten

## Zweck (1 Satz)

Die Dienste-Seite ist die zentrale Liste aller Dienste (Bestellungen, planbare Dienste,
Container, Abwesenheiten) — von hier aus werden Dienste erfasst, bearbeitet, zugeschnitten
(Shift-Cutting) und Container-Vorlagen gepflegt.

**Hinkommen:** Navi-Icon `open-shifts` in der linken Leiste (Tooltip „Alle Dienste",
Tastenkürzel Alt+4) oder direkt Route `/workplace/shift`.
Seiten-Überschrift: **Alle Dienste** (de: "Alle Dienste", en: "All Services",
fr: "Tous les services", it: "Tutti i servizi").

## Bereiche der Seite (in Render-Reihenfolge)

### 1. Card **Dienste** (de: "Dienste", en: "Services", fr: "Services", it: "Servizi")

Die Hauptliste. Oben rechts der Button **+ Dienst erfassen** (de: "+ Dienst erfassen",
en: "+ Create Shift", fr: "+ Créer un service", it: "Crea turno") — nur für Admins sichtbar
und nur im Filter-Modus „Bestellungen" (dort entstehen neue Dienste als Bestellung).

Je nach gewähltem Filter (rechte Card) zeigt die Tabelle eine andere Sicht:

- **Bestellungen** (filterType 0): Original-Tabelle ohne Abkürzungs-/Icon-Spalte.
  Aktionen pro Zeile: Bleistift (bearbeiten) + roter Papierkorb (löschen, mit
  Bestätigungsdialog). Ist die Checkbox „Nur versiegelte Bestellungen anzeigen" aktiv,
  erscheint stattdessen nur ein Info-Icon — versiegelte Bestellungen sind unveränderbar.
- **Planbare Dienste** (filterType 1): Tabelle mit Icon-Spalte und Schere pro Zeile —
  die Schere öffnet die Zuschnitt-Seite (Shift-Cutting) des Original-Dienstes.
- **Container** (filterType 2): gleiche Tabelle, statt Schere ein Box-Icon — öffnet die
  Schichtvorlagen-Seite (Container-Template) des Dienstes.

**Spalten** (Header klickbar = sortierbar, Pfeil zeigt Richtung): **ABK.** (de: "ABK.",
en: "ABBR.", fr: "ABR.", it: "ABBR.", nur in den planbaren/Container-Sichten), **NAME**
(de: "NAME", en: "NAME", fr: "NOM", it: "NOME"), **BESCHREIBUNG** (de: "BESCHREIBUNG",
en: "DESCRIPTION", fr: "DESCRIPTION", it: "DESCRIZIONE"), **VON DATUM / BIS DATUM**
(de: "VON DATUM"/"BIS DATUM", en: "FROM DATE"/"UNTIL DATE", fr: "DATE DE DÉBUT"/"DATE DE FIN",
it: "DATA DI INIZIO"/"DATA DI FINE" = Gültigkeit des Dienstes). Die zweite Zeile jedes
Eintrags zeigt **VON ZEIT / BIS ZEIT** (de: "VON ZEIT"/"BIS ZEIT", en: "FROM TIME"/"UNTIL TIME",
fr: "HEURE DE DÉBUT"/"HEURE DE FIN", it: "ORA DI INIZIO"/"ORA DI FINE") sowie Checkboxen für
die Wochentage MO–SO plus **FT** (Feiertag; de: "FT", en: "HOL", fr: "JF", it: "FER") und
**+FT** (Wochentag und Feiertag; de: "+FT", en: "+HOL", fr: "+JF", it: "+FER").

**Icon-Spalte** (planbare/Container-Sicht): kennzeichnet den Diensttyp — Fragezeichen-Uhr =
sporadischer Dienst, Zeitfenster-Icon = TimeRange-Dienst, Segment-Icon = zugeschnittener
Dienst (SplitShift), Box = Container. Unter der Tabelle sitzt die Seiten-Navigation
(Pagination) mit wählbarer Zeilenzahl.

### 2. Filter-Card rechts (id `shift-nav`)

- Dropdown **Gültigkeit** (de: "Gültigkeit", en: "Validity", fr: "Validité", it: "Validità")
  mit den Checkboxen **Aktive** (en: "Active"), **Einstige** (en: "Former") und
  **Zukünftige** (en: "Future") — filtert nach Gültigkeitszeitraum der Dienste.
- Radio-Auswahl der Sicht (nur für Admins änderbar): **Bestellungen** (de: "Bestellungen",
  en: "Orders", fr: "Commandes", it: "Ordini"), **Planbare Dienste** (de: "Planbare Dienste",
  en: "Plannable Services", fr: "Services planifiables", it: "Servizi Pianificabili"),
  **Container** (de: "Container", en: "Container", fr: "Conteneur", it: "Contenitore") und
  **Abwesenheit** (de: "Abwesenheit", en: "Absence", fr: "Absence", it: "Assenza").
- Bei „Bestellungen": Checkbox **Nur versiegelte Bestellungen anzeigen** (en: "Show Only
  Sealed Orders"). Bei „Planbare Dienste": Zusatz-Checkboxen **Zeitbereich** (en: "Time Range")
  und **Sporadisch** (en: "Sporadic") zum Eingrenzen auf diese Diensttypen.

## Unterseiten

- **Dienst bearbeiten** (`/workplace/edit-shift/:id`, neu via `/workplace/new-shift`,
  Admin-only): Cards **Allgemeines** (en: "General" — Name, Abkürzung, von/bis, Schalter
  „Experten Modus" und „Ist ein Container"), **Gruppe** (en: "Group"), **Erforderliche
  Qualifikationen** (en: "Required Qualifications"), **Stunden und Wochentage** (en: "Hours
  and Weekdays" — Zeiten, Wochentage, Zeitrahmen-Option), **Macro** (nur Experten-Modus),
  **Adresse** (en: "Address", nicht bei Containern), **Spezielle Merkmale** (en: "Special
  Features" — „sporadischer Einsatz", „Anzahl Mitarbeiter pro Schicht", „Anzahl Aufgabe pro
  Schicht") und **Standard-Spesen** (en: "Default Expenses").
- **Zuschnitt** (de: "Zuschnitt", en: "Split Services"; `/workplace/cut-shift/:id`, via
  Schere): Card **Liste der zu teilenden Dienste** (en: "List of Services to Split") mit den
  Buttons **Nach Datum trennen**, **Nach Zeit trennen**, **Nach Wochentagen trennen**,
  **Nach Personal trennen**, **Nach Aufgabe trennen** und **Zurücksetzen** — zerlegt einen
  Dienst in Teilstücke (Spalten u. a. MA. = Personal, ANZ. = Aufgaben). Im Szenario-Modus
  sind die Buttons gesperrt.
- **Schichtvorlage** (de: "Schichtvorlage", en: "Shift Template";
  `/workplace/container-template/:id`, via Box-Icon): Wochentags-Auswahl, Zeitlineal
  (Time-Ruler), Card **Zugewiesene Aufgaben** (en: "Assigned Tasks") — verfügbare
  Task-Dienste per Drag & Drop zuweisen, inkl. Routen-Optimierung und PDF-Export.

## Typische Aufgaben (+ passende Klacksy-Skills)

- Dienst suchen oder Details nachschlagen → `search_shifts`, `get_shift_details`
- Neuen Dienst als Bestellung erfassen → `create_shift`
- Dienst ändern oder löschen → `update_shift`, `delete_shift`
- Qualifikationsanforderung am Dienst setzen → `set_shift_required_qualification`
- Versiegelte Bestellungen einsehen → `list_sealed_orders`
- Liste mit Filter öffnen / hinnavigieren → `search_in_list`, `navigate_to`

## Verwandte Skills / Seiten

- `explain_shift_lifecycle_order_to_shift` — warum Bestellung → versiegelt → planbarer Dienst
- `explain_shift_sporadic`, `explain_shift_time_range`, `explain_shift_container` — die Diensttypen
- `explain_macro_editor` — Macros für die Dauer-/Lohnberechnung am Dienst
- Schedule-Seite (`/workplace/schedule`) — dort werden die planbaren Dienste gebucht

## Trigger-Phrasen

- "Was sehe ich auf der Dienste-Seite?"
- "Was bedeuten die Icons in der Dienstliste?"
- "Wie schneide ich einen Dienst zu?" / "Wozu ist die Schere?"
- "Was ist der Unterschied zwischen Bestellungen und planbaren Diensten?"
- "What do I see on the shifts page and what do the columns mean?"
