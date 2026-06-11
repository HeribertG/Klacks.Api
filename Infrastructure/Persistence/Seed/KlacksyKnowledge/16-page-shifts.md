---
name: explain_page_shifts
description: |
  Explains the Services page (Dienste / All Services) at /workplace/shift and its sub-pages
  edit-shift (/workplace/edit-shift/:id, /workplace/new-shift), shift cutting
  (/workplace/cut-shift/:id) and the container template (/workplace/container-template/:id).
  Covers the service list card with its filter views Orders (incl. the sealed-orders checkbox),
  Plannable Services (with Time Range and Sporadic sub-filters), Container and Absence, the
  table columns (abbreviation, name, description, validity dates, times, weekday/holiday
  checkboxes), the type icons, the row action icons (pencil, trash, scissors, box), the edit
  mask cards (General with expert mode and the lock/seal button, Group, Required Qualifications,
  Hours and Weekdays incl. isMonday–isSunday, Macro, Address, Special Features with sporadic
  scope, employees per shift and tasks per shift, Default Expenses), the cut page with its
  cut-by-date/time/weekdays/staff/task buttons, and the container template with weekday
  selection, time ruler, drag & drop task assignment and route optimization. Use this when the
  user asks what they see on the Shifts page, what the cards/columns/icons mean, or how to work
  with it. Supports a level parameter: short (purpose only), elements (every element explained),
  effects (data sources, the order→sealed→plannable lifecycle, group scope, scenario isolation
  and the resource monitor impact).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - shifts
  - dienste
  - dienstliste
  - shift list
  - bestellungen
  - orders
  - planbare dienste
  - plannable services
  - schichtvorlage
  - shift template
  - container template
  - containervorlage
  - zuschnitt
  - cut shift
  - split shift
  - dienst trennen
  - sealed order
  - sealed orders
  - versiegelte bestellungen
  - dienst versiegeln
  - lock shift
  - sporadischer dienst
  - sporadic shift
  - zeitbereich
  - time range
  - experten modus
  - expert mode
  - services page
  - alle dienste
  - dienst erfassen
  - create shift
  - anzahl mitarbeiter pro schicht
synonyms:
  de: [was sehe ich hier, erkläre diese seite, was bedeutet diese karte, was bedeutet diese tabelle, dienste-seite, alle dienste, dienstliste, schichtliste, was bedeuten die icons, wie schneide ich einen dienst zu, wozu ist die schere, dienst versiegeln, was ist eine versiegelte bestellung, was ist ein container, sporadischer einsatz, dienst im zeitrahmen, warum kann ich den dienst nicht bearbeiten]
  en: [what do i see here, explain this page, what does this card mean, what does this table mean, services page, all services, shift list, shifts overview, what do the icons mean, how do i cut a shift, what is the scissors for, seal an order, what is a sealed order, what is a container, sporadic shift, time range shift, why can't i edit this shift]
  fr: [que vois-je ici, explique cette page, que signifie cette carte, page services, tous les services, liste des services, que signifient les icônes, comment découper un service, à quoi sert le ciseau, commande scellée, qu'est-ce qu'un conteneur, service sporadique, plage horaire, pourquoi je ne peux pas modifier ce service]
  it: [cosa vedo qui, spiega questa pagina, cosa significa questa scheda, pagina servizi, tutti i servizi, elenco dei servizi, cosa significano le icone, come suddivido un servizio, a cosa servono le forbici, ordine sigillato, cos'è un contenitore, servizio sporadico, fascia oraria, perché non posso modificare questo servizio]
---

# Dienste-Seite — /workplace/shift mit edit-shift, cut-shift und container-template

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Die Dienste-Seite mit der Überschrift **Alle Dienste** (de: "Alle Dienste", en: "All
Services", fr: "Tous les services", it: "Tutti i servizi") ist die zentrale Liste aller
Dienste — Bestellungen, planbare Dienste und Container. Von hier aus werden Dienste als
Bestellung erfasst, bearbeitet, versiegelt, zugeschnitten (Shift-Cutting) und
Container-Vorlagen gepflegt. Erreichbar über das Navi-Icon `open-shifts` in der linken
Leiste (Tooltip "Alle Dienste", Alt+4) oder direkt über die Route `/workplace/shift`;
Unterseiten sind `/workplace/edit-shift/:id` (neu: `/workplace/new-shift`),
`/workplace/cut-shift/:id` (Zuschnitt) und `/workplace/container-template/:id`
(Schichtvorlage).

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand, wirkt auf diese Seite)

- **Suche** (Suchfeld in der Kopfleiste): filtert die Dienstliste nach **Name oder
  Abkürzung** des Dienstes. Nur auf dieser Seite zeigt die Suche zusätzlich die Checkbox
  **inklusive Kundenname** (de: "inklusive Kundenname", en: "Include client name", fr:
  "Inclure le nom du client", it: "incluso il nome del cliente") — dann wird auch in
  Vorname, Nachname, Name, Ledigname und Firma des zugeordneten Kunden gesucht. Mehrere
  Wörter werden UND-verknüpft; mit `+` verbundene Begriffe ODER-verknüpft; ein einzelnes
  Zeichen sucht nach Anfangsbuchstaben.
- **Gruppen-Auswahl** (Dropdown mit Gruppenbaum): Der globale Gruppen-Scope wirkt auf diese
  Seite — die Wahl setzt den Gruppenfilter der Dienstliste und lädt sie neu. Besonderheit:
  In der Sicht "Bestellungen" (ohne Versiegelt-Häkchen) wird der Gruppenfilter NICHT
  angewendet; in den anderen Sichten zeigt er die Dienste der gewählten Gruppe und
  zusätzlich alle Dienste ganz ohne Gruppenzuordnung.

### Card **Dienste** (de: "Dienste", en: "Services", fr: "Services", it: "Servizi") — die Hauptliste links

Oben rechts der Button `shift-create-btn` **+ Dienst erfassen** (de: "+ Dienst erfassen",
en: "+ Create Shift", fr: "+ Créer un service", it: "Crea turno") — nur für Admins sichtbar
und nur in der Sicht "Bestellungen" (neue Dienste entstehen immer als Bestellung; öffnet
`/workplace/new-shift`).

Je nach gewählter Sicht (Filter-Card rechts) zeigt die Card eine andere Tabelle:

- **Bestellungen** (filterType 0, Tabelle `original-shift-table`): ohne Abkürzungs- und
  Icon-Spalte — nur Name, Beschreibung, Datums-/Zeitspalten und Wochentage. Aktionen pro
  Zeile: Bleistift (bearbeiten) und roter Papierkorb (löschen, mit Bestätigungsdialog
  "Möchten Sie '...' wirklich löschen?"). Ist die Checkbox "Nur versiegelte Bestellungen
  anzeigen" aktiv, zeigt die Tabelle die versiegelten Bestellungen und statt der Aktionen
  nur ein Info-Icon — versiegelte Bestellungen sind unveränderbar. Inhalt: Aufgaben-Dienste
  im Status Bestellung bzw. mit Häkchen die versiegelten Bestellungen.
- **Planbare Dienste** (filterType 1, Tabelle `shift-detail-table`): mit Icon- und
  ABK.-Spalte. Aktionen pro Zeile: Schere `shift-cut-btn-{id}` (öffnet den Zuschnitt des
  zugehörigen Original-Dienstes) und Bleistift `shift-edit-btn-{id}`. Inhalt:
  planbare Schichten und ihre zugeschnittenen Teilstücke.
- **Container** (filterType 2): gleiche Tabelle, statt der Schere ein Box-Icon
  `shift-container-btn-{id}` — öffnet die Schichtvorlage (Container-Template) des Dienstes.
  Inhalt: planbare Container-Dienste.
- **Abwesenheit** (filterType 3): derzeit ohne Inhalt — das Backend liefert für diese Sicht
  immer eine leere Liste und es wird keine Tabelle gerendert (Platzhalter-Sicht).

**Spalten** (Header klickbar = sortierbar mit Dreiwege-Sortierung auf/ab/aus, Pfeil zeigt
die Richtung): **ABK.** (de: "ABK.", en: "ABBR.", fr: "ABR.", it: "ABBR.", nur in den
planbaren/Container-Sichten), **NAME** (de: "NAME", en: "NAME", fr: "NOM", it: "NOME"),
**BESCHREIBUNG** (de: "BESCHREIBUNG", en: "DESCRIPTION", fr: "DESCRIPTION", it:
"DESCRIZIONE"), **VON DATUM / BIS DATUM** (de: "VON DATUM"/"BIS DATUM", en: "FROM
DATE"/"UNTIL DATE", fr: "DATE DE DÉBUT"/"DATE DE FIN", it: "DATA DI INIZIO"/"DATA DI FINE"
= Gültigkeit des Dienstes). Die zweite Zeile jedes Eintrags zeigt **VON ZEIT / BIS ZEIT**
(de: "VON ZEIT"/"BIS ZEIT", en: "FROM TIME"/"UNTIL TIME", fr: "HEURE DE DÉBUT"/"HEURE DE
FIN", it: "ORA DI INIZIO"/"ORA DI FINE") sowie schreibgeschützte Checkboxen für die
Wochentage MO–SO plus **FT** (de: "FT", en: "HOL", fr: "JF", it: "FER" = Feiertag, egal an
welchem Wochentag) und **+FT** (de: "+FT", en: "+HOL", fr: "+JF", it: "+FER" = Feiertag an
selektiertem Wochentag).

**Icon-Spalte** (nur planbare/Container-Sicht), kennzeichnet den Diensttyp:
Fragezeichen-Uhr = sporadischer Dienst (isSporadic), Zeitfenster-Icon = TimeRange-Dienst
(isTimeRange, nur wenn nicht sporadisch), Segment-Icon = regulärer bzw. zugeschnittener
Dienst, Box = Container-Sicht. Unter der Tabelle sitzt die Seiten-Navigation (Pagination)
mit wählbarer Zeilenzahl inkl. Automatik (passt die Zeilenzahl an die Fensterhöhe an).

### Filter-Card rechts (id `shift-nav`, Formular `navShiftForm`)

- Dropdown **Gültigkeit** (de: "Gültigkeit", en: "Validity", fr: "Validité", it:
  "Validità") mit den Checkboxen **Aktive** (en: "Active"), **Einstige** (en: "Former") und
  **Zukünftige** (en: "Future") — filtert nach dem Gültigkeitszeitraum der Dienste.
- Radio-Auswahl der Sicht (IDs `shift-filter-original`, `shift-filter-shift`,
  `shift-filter-container`, `shift-filter-absence`; nur für Admins änderbar):
  **Bestellungen** (de: "Bestellungen", en: "Orders", fr: "Commandes", it: "Ordini"),
  **Planbare Dienste** (de: "Planbare Dienste", en: "Plannable Services", fr: "Services
  planifiables", it: "Servizi Pianificabili"), **Container** (de: "Container", en:
  "Container", fr: "Conteneur", it: "Contenitore"), **Abwesenheit** (de: "Abwesenheit",
  en: "Absence", fr: "Absence", it: "Assenza"). Nicht-Admins werden beim Laden fest auf
  "Planbare Dienste" gesetzt.
- Bei "Bestellungen": Checkbox **Nur versiegelte Bestellungen anzeigen** (de: "Nur
  versiegelte Bestellungen anzeigen", en: "Show Only Sealed Orders", fr: "Afficher
  uniquement les commandes scellées").
- Bei "Planbare Dienste": Zusatz-Checkboxen **Zeitbereich** (de: "Zeitbereich", en: "Time
  Range", fr: "Plage horaire", it: "Fascia oraria") und **Sporadisch** (de: "Sporadisch",
  en: "Sporadic", fr: "Sporadique", it: "Sporadico"). Beide an (Standard) = alle planbaren
  Dienste; nur eine an = nur dieser Diensttyp; beide aus = nur reguläre Dienste ohne
  Zeitbereich und ohne Sporadisch.

### Unterseite **Dienst bearbeiten** (`/workplace/edit-shift/:id`, neu `/workplace/new-shift`)

Seiten-Überschrift **neuer Dienst** (de: "neuer Dienst", en: "New shift", fr: "Nouveau
service", it: "Nuovo turno"). Die Kopfleisten-Suche ist auf dieser Unterseite ausgeblendet;
Speichern/Verwerfen läuft über die Fusszeile des Arbeitsbereichs (mit Query-Parameter
`readonly=true` ist sie ausgeblendet). Cards in dieser Reihenfolge:

1. **Allgemeines** (de: "Allgemeines", en: "General", fr: "Général", it: "Generale") —
   Schalter **Experten Modus** (de: "Experten Modus", en: "Expert Mode", fr: "Mode expert")
   blendet die Experten-Cards ein (die Wahl wird im Browser gemerkt; beim Ausschalten wird
   die Zeitrahmen-Option am Dienst zurückgesetzt). Felder: **Abkürzung*** (Input
   `abbreviation`, `data-klacksy-target="shift-form.abbreviation"`, max. 6 Zeichen — wird
   beim Tippen des Namens automatisch vorgeschlagen, solange das Feld unberührt ist),
   **Name*** (Input `name`, `data-klacksy-target="shift-form.name"`), **Von Datum*** /
   **Bis Datum** (Datepicker) und eine Notiz (Rich-Text-Editor). Rechts der **Lock-Button**
   `shift-lock-btn` (grün, offenes Schloss) — nur im Status Bestellung
   sichtbar; Tooltip: "Nach Sperrung ist der Auftrag unveränderlich und steht zur Planung
   bereit." Er wird erst aktiv, wenn Abkürzung, Name, Von-Datum, mindestens ein Wochentag,
   mindestens eine Gruppe, Anzahl Aufgaben > 0 und Anzahl Mitarbeiter > 0 gültig sind.
   Klick setzt den Status auf versiegelt — nach dem Speichern ist das nicht
   umkehrbar. In jedem anderen Status erscheint statt des Buttons ein geschlossenes Schloss
   und alle Felder sind deaktiviert. Checkbox **Ist ein Container** (de: "Ist ein
   Container", en: "Is a container", fr: "Est un conteneur", it: "È un contenitore") — nur
   im Experten-Modus und nur im Status Bestellung sichtbar; aktivieren leert Kunde/Adresse
   und schaltet die Zeitrahmen-Option aus.
2. **Gruppe** (de: "Gruppe", en: "Group", fr: "Groupe", it: "Gruppo") — Zuordnung des
   Dienstes zu Gruppen; Card erscheint nur, wenn Gruppen existieren.
3. **Erforderliche Qualifikationen** (de: "Erforderliche Qualifikationen", en: "Required
   Qualifications", fr: "Qualifications requises", it: "Qualifiche richieste") — Filter
   nach Typ/Kategorie/Land, Plus-Button, Tabelle mit **QUALIFIKATION** (en:
   "QUALIFICATION"), **MINDESTSTUFE** (en: "MIN LEVEL") und **PFLICHT** (en: "MANDATORY").
4. **Stunden und Wochentage** (de: "Stunden und Wochentage", en: "Hours and Weekdays", fr:
   "Heures et jours de la semaine", it: "Ore e Giorni Feriali") — **Von Zeit hh:mm**, **Bis
   Zeit hh:mm**, **Dauer** (en: "Duration"); im Experten-Modus (nicht bei Containern)
   zusätzlich die Zeitrahmen-Checkbox "Dienst innerhalb des Zeitrahmens (Von Zeit hh:mm -
   Bis Zeit hh:mm). Bitte in Feld Dauer die Dauer des Diensten angeben" (= TimeRange:
   der Dienst liegt flexibel in diesem Zeitfenster, die Dauer zählt). Darunter die
   Wochentags-Checkboxen `isMonday`–`isSunday` (Montag–Sonntag) plus **Feiertag, egal an
   welchen Wochentag** (`isHoliday`) und **Feiertag an selektiertem Wochentag**
   (`isWeekdayAndHoliday`).
5. **Macro** (de: "Macro", en: "Macro") — nur im Experten-Modus sichtbar; Macro-Zuordnung
   für die Dauer-/Lohnberechnung des Dienstes.
6. **Adresse** (de: "Adresse", en: "Address", fr: "Adresse", it: "Indirizzo") — nicht bei
   Containern; Suche und Auswahl des Kunden bzw. der Einsatzadresse.
7. **Spezielle Merkmale** (de: "Spezielle Merkmale", en: "Special Features", fr:
   "Caractéristiques spéciales", it: "Caratteristiche Speciali") — nur im Experten-Modus
   oder bei Containern: Checkbox **sporadischer Einsatz** (de: "sporadischer Einsatz", en:
   "Sporadic use", fr: "Utilisation sporadique", it: "Utilizzo sporadico") mit
   Periodizitäts-Dropdown (wöchentlich, monatlich, Quartal, Semester, jährlich, Laufzeit
   des Vertrages); **Briefing**/**Debriefing** und **Anreisezeit**/**Rückreisezeit** (en:
   "Travel time before"/"Travel time after"); **Anzahl Mitarbeiter pro Schicht** (en:
   "Number of employees per shift", Feld `sumEmployees`, 1–100) und **Anzahl Aufgabe pro
   Schicht** (en: "Number of tasks per shift", Feld `sumQuantity`, 1–100). Bei Containern
   ist nur die Sporadisch-Option sichtbar.
8. **Standard-Spesen** (de: "Standard-Spesen", en: "Default Expenses", fr: "Frais par
   défaut", it: "Spese predefinite") — nur Experten-Modus, nicht bei Containern;
   Spesenpositionen mit Beschreibung, Betrag, steuerbar.

Rechts eine Filter-Spalte (ausgeblendet bei zugeschnittenen Diensten und Containern): sie
filtert die Kunden-Suche der Adresse-Card — Checkboxen Frau/Mann/juristische Person,
Gültigkeit der Mitgliedschaft (Aktive/Einstige/Zukünftige), Länder, Kantone,
Ein-/Austritts-Zeitraum und "Filter zurücksetzen".

**Bedeutung der Spezialtypen:** sporadisch = unregelmässiger Einsatz auf Abruf; TimeRange =
flexibel innerhalb eines Zeitfensters. Beide zählen NICHT in den Dienste-Balken des
Ressourcen-Monitors auf dem Dashboard.

### Unterseite **Zuschnitt** (`/workplace/cut-shift/:id`, via Schere)

Seiten-Überschrift **Zuschnitt** (de: "Zuschnitt", en: "Split Services", fr: "Découpage",
it: "Servizi suddivisi"), Card **Liste der zu teilenden Dienste** (de: "Liste der zu
teilenden Dienste", en: "List of Services to Split", fr: "Liste des services à découper",
it: "Elenco dei servizi da suddividere"). Button-Reihe (erst nach Klick auf eine
Tabellenzeile aktiv, und nur wenn die jeweilige Trennung beim selektierten Teilstück
möglich ist; jeder Button öffnet ein Modal):

- `cut-date-btn` **Nach Datum trennen** (en: "Cut Date") — Datums-Picker mit Min/Max.
- `cut-time-btn` **Nach Zeit trennen** (en: "Cut Time") — Uhrzeit hh:mm.
- `cut-weekdays-btn` **Nach Wochentagen trennen** (en: "Cut Weekdays") — Checkboxen nur für
  die beim Dienst aktiven Wochentage.
- `cut-staff-btn` **Nach Personal trennen** (en: "Cut Staff") — Anzahl mit Min/Max.
- `cut-task-btn` **Nach Aufgabe trennen** (en: "Cut Task") — Anzahl mit Min/Max.
- `reset-cuts-btn` **Zurücksetzen** (en: "Reset") — setzt die Trennungen ab einem neuen
  Startdatum zurück ("Neues Startdatum wählen"); nur aktiv, wenn zugeschnittene Teilstücke
  existieren und alles gespeichert ist.

Tabelle der Teilstücke: **ABK.**, **NAME**, **BESCHREIBUNG**, **VON DATUM / BIS DATUM**,
**MA.** (de: "MA.", en: "EMP.", fr: "PERS.", it: "DIP." = Mitarbeiter) sowie in der zweiten
Zeile **VON ZEIT / BIS ZEIT**, Wochentage MO–SO, FT, +FT und **ANZ.** (de: "ANZ.", en:
"QTY.", fr: "QTÉ.", it: "QTÀ." = Aufgaben). Die Zellen Abkürzung, Name und Beschreibung
sind direkt in der Tabelle editierbar (Klick auf die Zelle der selektierten Zeile, Enter =
übernehmen). Im Szenario-Modus sind alle Zuschnitt-Buttons gesperrt (Tooltip:
"Schicht-Bearbeitung im Szenario nicht möglich. Szenario übernehmen oder verwerfen, um
Änderungen zu speichern.").

### Unterseite **Schichtvorlage** (`/workplace/container-template/:id`, via Box-Icon)

Kopfbereich: Name des Containers (sonst **Schichtvorlage**, de: "Schichtvorlage", en:
"Shift Template", fr: "Modèle de service", it: "Modello di turno"), Zeitfelder **Von / Bis
/ Dauer**, Wochentags-Radio-Buttons (nur die im Container aktivierten Wochentage), optional
die Checkboxen **Feiertag** und **Wochentag oder Feiertag** (falls am Container aktiviert),
Auswahl **Start**/**Ende**-Basis (Adressen) und **Transport**-Modus (Auto, Velo, zu Fuss,
Mix — Velo/Fuss/Mix benötigen einen OpenRouteService-API-Key). Drei Zonen mit
verschiebbaren Trennlinien:

- **Links: Zeitlineal (Time-Ruler)** — visuelle Tagesachse der zugewiesenen Aufgaben;
  Rechtsklick auf einen Eintrag öffnet das Kontextmenü mit **Eigenschaften...** (Modal mit
  Briefing-/Debriefing-Zeit, Anreise-/Rückreisezeit, bei TimeRange-Diensten der Startzeit
  im Zeitfenster, bei Mix-Transport dem Transportmittel pro Eintrag).
- **Oben rechts: Card "Zugewiesene Aufgaben"** (de: "Zugewiesene Aufgaben", en: "Assigned
  Tasks", fr: "Tâches assignées", it: "Compiti assegnati") — Tabelle `selected-tasks-table`
  mit Name, Abkürzung, Zeit, Arbeitszeit, Kunde und Papierkorb pro Zeile (plus
  Alle-entfernen im Header). Werkzeuge: Zauberstab-Icon = Autofill ("Dienste automatisch
  zuordnen und Route optimieren") mit Toleranz-Slider (flexibel ↔ exakt), Routen-Icon =
  "Route optimieren", Route-als-PDF-Export, **Dienste als PDF exportieren** und ein
  Kompakt-Menü ("Dienste aneinanderreihen / Lücken entfernen", mit oder ohne Reisezeit).
- **Unten rechts:** Tabs pro Vorlagen-Zeile des gewählten Wochentags plus der permanente
  Tab **Beschäftigung** (de: "Beschäftigung", en: "Employment", fr: "Emploi", it:
  "Impiego") mit Absenz-Arten als Drag-Quellen; daneben die Tabelle `available-tasks-table`
  der verfügbaren Task-Dienste (sortierbar nach Name/Abkürzung/Zeit/Arbeitszeit/Kunde, mit
  Typ-Icons). Zuweisen per **Drag & Drop** in die Card "Zugewiesene Aufgaben". Die
  Kopfleisten-Suche filtert hier clientseitig die verfügbaren Aufgaben nach Name, Abkürzung
  oder Kunde.

Die Vorlage ist durch eine Sperre geschützt: Hält ein anderer Benutzer (oder ein zweites
eigenes Browser-Tab) dieselbe Vorlage offen, erscheint ein Hinweis-Banner und die Seite ist
schreibgeschützt.

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenbasis & Ausschlüsse**: Die Liste zeigt nur echte Dienste — Szenario-Zeilen des
  Planungs-Assistenten (mit AnalyseToken oder Szenario-Quelle) sind in allen Sichten und
  auch im Zuschnitt immer ausgeblendet; gelöschte Dienste (Soft-Delete) erscheinen nicht.
- **Lebenszyklus Bestellung → versiegelt → planbar**: Eine Bestellung wird im Edit über den
  Lock-Button versiegelt (unveränderlich). Beim Speichern erzeugt das Backend
  automatisch eine planbare Schicht mit Verweis auf die versiegelte
  Bestellung. Der Zuschnitt zerlegt diese planbare Schicht in Teilstücke — die
  Schere in der Liste führt deshalb immer zum Original-Dienst. Details: Skill
  `explain_shift_lifecycle_order_to_shift`.
- **Bearbeitbarkeit**: Felder der Edit-Maske sind nur im Status Bestellung
  änderbar; versiegelte Bestellungen, planbare Kopien und Teilstücke sind dort
  schreibgeschützt (Namens-/Beschreibungs-Korrekturen an Teilstücken gehen über die
  editierbaren Zellen der Zuschnitt-Tabelle).
- **Einsatzplanung (`/workplace/schedule`)**: Die planbaren Dienste (inkl. Teilstücke und
  Container) sind das Angebot, das im Schichtplan gebucht wird. Gruppenfremd gebuchte
  Dienste erscheinen dort als versiegelt (sealed) markiert, werden aber nie hart
  ausgefiltert. Tagsperren/Periodenabschluss wirken auf die Einsatzplanung, nicht auf die
  Dienstliste selbst.
- **Ressourcen-Monitor (Dashboard)**: Der Dienste-Balken zählt OHNE sporadische Dienste,
  OHNE TimeRange-Dienste, OHNE versiegelte Bestellungen und ohne Szenario-Zeilen; ein
  Container zählt als 1 Dienst. Wer sporadische oder TimeRange-Dienste anlegt, sieht sie
  dort also bewusst nicht in der Kapazitätskurve.
- **Szenarien**: Im aktiven Analyse-Szenario sind die Zuschnitt-Buttons gesperrt;
  Szenario-Kopien von Diensten bleiben unsichtbar, bis das Szenario übernommen wird.
- **Gruppen-Scope**: Die globale Gruppen-Auswahl wirkt seitenübergreifend (auch Schichtplan,
  Absenzen, Verfügbarkeit). Auf dieser Seite gilt sie für die planbaren/Container-Sichten;
  Dienste ohne Gruppenzuordnung bleiben immer sichtbar, die Bestellungen-Sicht (ohne
  Versiegelt-Häkchen) ignoriert den Gruppenfilter ganz.
- **Berechtigungen**: Nicht-Admins sehen nur die Sicht "Planbare Dienste" (Radios und
  Versiegelt-Checkbox deaktiviert, beim Laden erzwungen) und haben keinen
  "+ Dienst erfassen"-Button.
- **Stammdaten-Wirkung**: Gruppen-Zuordnung, erforderliche Qualifikationen, Anzahl
  Mitarbeiter/Aufgaben pro Schicht und Macros am Dienst steuern die Vorschläge und
  Prüfungen des Planungs-Assistenten und der Einsatzplanung.

### Typische Aufgaben

- Dienst suchen oder Details nachschlagen — Skills `search_shifts`, `get_shift_details`
- Neuen Dienst als Bestellung erfassen — Skill `create_shift`
- Dienst ändern oder löschen — Skills `update_shift`, `delete_shift`
- Qualifikationsanforderung am Dienst setzen — Skill `set_shift_required_qualification`
- Versiegelte Bestellungen einsehen — Skill `list_sealed_orders`
- Dienstliste mit Filter öffnen — Skill `search_in_list` (entityType "shift",
  searchQuery = Name/Abkürzung)
- Liste auf eine Gruppe eingrenzen oder wieder alle zeigen — Skill `select_group`
- Zur Seite springen — Skill `navigate_to` (Ziel "shifts")

### Verwandte Seiten

- `explain_shift_lifecycle_order_to_shift` — warum Bestellung → versiegelt → planbarer Dienst
- `explain_shift_sporadic`, `explain_shift_time_range`, `explain_shift_container` — die Diensttypen im Detail
- `explain_macro_editor` — Macros für die Dauer-/Lohnberechnung am Dienst
- `/workplace/schedule` — Einsatzplanung; dort werden die planbaren Dienste gebucht
- Dashboard (`explain_page_dashboard`) — Ressourcen-Monitor mit dem Dienste-Balken

### Trigger-Phrasen

- "Was sehe ich auf der Dienste-Seite?"
- "Was bedeuten die Icons in der Dienstliste?"
- "Wie schneide ich einen Dienst zu?" / "Wozu ist die Schere?"
- "Was ist der Unterschied zwischen Bestellungen und planbaren Diensten?"
- "Warum kann ich diesen Dienst nicht mehr bearbeiten?" (versiegelt/planbar)
- "What do I see on the shifts page and what do the columns mean?"
