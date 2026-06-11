---
name: explain_page_absence
description: |
  Explains the Absence Calendar page (Absenzen Kalender) at /workplace/absence — a year-wide
  Gantt chart with one row per employee and one column per calendar day. Covers the header
  (draggable absence-type chips with colors and filter checkmarks, PDF export, year counter,
  zoom slider), the row header with sort/filter popup, the Gantt surface (weekend/holiday
  shading, colored absence bars, drag & drop creation, move/resize via anchors, context menu
  with copy/cut/paste/delete/convert), and the lower detail mask with the Mask and List tabs.
  Use this when the user asks what they see on the Absence Calendar page, what the
  cards/charts/columns mean, or how to work with it. Supports a level parameter:
  short (purpose only), elements (every element explained), effects (data sources and
  how the page interacts with schedule, settings, period closing and the dashboard).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - absence calendar
  - absenzen kalender
  - absence gantt
  - abwesenheitskalender
  - absenz eintragen
  - ferienkalender
  - absence page
  - gantt
  - jahreskalender
  - absenz umwandeln
  - convert absence
  - gebucht
  - booked
  - ferien
  - vacation
  - krankheit
  - sickness
  - feiertage
  - holidays
  - wochenende
  - weekend
  - absenz verschieben
  - absenz löschen
synonyms:
  de: [was sehe ich hier, erkläre diese seite, was bedeutet dieses diagramm, was bedeutet diese karte, absenzen kalender, abwesenheitskalender, absenz eintragen, ferien eintragen, absenz verschieben, absenz umwandeln, warum kann ich nicht löschen, krankheit erfassen, was bedeutet gebucht, feiertage im kalender]
  en: [what do i see here, explain this page, what does this chart mean, what does this card mean, absence calendar, absence gantt, enter absence, vacation calendar, move absence, convert absence, why can't i delete, record sickness, what does booked mean, holidays in the calendar]
  fr: [que vois-je ici, explique cette page, que signifie ce diagramme, calendrier d'absences, saisir une absence, congés, gantt des absences, déplacer une absence, convertir une absence, pourquoi je ne peux pas supprimer, que signifie réservé]
  it: [cosa vedo qui, spiega questa pagina, cosa significa questo diagramma, calendario assenze, inserire un'assenza, ferie, gantt delle assenze, spostare un'assenza, convertire un'assenza, perché non posso eliminare, cosa significa prenotato]
---

# Absenzen Kalender — die Seite /workplace/absence

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Der **Absenzen Kalender** (de: "Absenzen Kalender", en: "Absence Calendar", fr: "Calendrier
d'absences", it: "Calendario assenze") zeigt ein ganzes Kalenderjahr als Gantt-Diagramm —
eine Zeile pro Mitarbeiter, eine Spalte pro Tag — und dient zum Vorplanen, Verschieben und
Bearbeiten von Absenzen (Ferien, Krankheit usw.). Hier erfasste Absenzen sind vorgeplante
Platzhalter, die der Planung als Erinnerungsstütze dienen; zusätzlich zeigt der Kalender die
in der Einsatzplanung gebuchten Absenz-Dienste. Erreichbar über das Navi-Icon
`open-absences` (Gantt-Symbol, Alt+2) oder die Route `/workplace/absence`.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand, wirkt auf diese Seite)

- **Suche** (Suchfeld in der Kopfleiste): filtert auf dieser Seite die Mitarbeiterzeilen des
  Kalenders nach **Name oder ID-Nummer** — der eingegebene Begriff wird als Suchfilter an die
  Jahres-Abfrage übergeben und die Zeilenliste neu geladen; Leeren der Suche zeigt wieder alle
  Mitarbeitenden.
- **Gruppen-Auswahl** (Dropdown mit Gruppenbaum, Option **Alle Gruppen** (de: "Alle Gruppen",
  en: "All groups", fr: "Tous les groupes", it: "Tutti i gruppi")): begrenzt den Kalender auf
  die Mitarbeitenden der gewählten Gruppe (im Baum sind auch Untergruppen wählbar). Die Wahl
  ist global und gilt gleichzeitig für alle Arbeitsbereich-Seiten.

### Kopfzeile (oben)

- **Absenz-Arten-Legende**: alle konfigurierten Absenz-Arten als farbige Chips (Farbe = Farbe
  der Balken im Diagramm). Klick auf das Häkchen einer Art blendet deren Einträge im ganzen
  Diagramm ein oder aus (Legenden-Filter). Ein Chip lässt sich per **Drag & Drop** auf eine
  Mitarbeiterzeile ziehen — das erzeugt direkt einen neuen Eintrag mit der Standardlänge der Art.
- **PDF-Export-Button**: exportiert das ganze Jahres-Gantt-Diagramm als PDF (für die Liste
  einer einzelnen Person gibt es einen zweiten PDF-Export im Tab "Liste" der Detail-Maske).
- **Jahr** (de: "Jahr:", en: "Year:", fr: "Année:", it: "Anno:"): Zähler zum Wechseln des
  angezeigten Jahres.
- **Zoom-Slider**: 50–300 % (Schritte von 10 %), vergrößert/verkleinert die Tagesspalten.

### Zeilen-Header (links)

Zeigt die Mitarbeitenden (eine Zeile pro Person). Das Filter-Icon öffnet ein Popup mit
Sortierung nach Vorname / Name / Firma / Stunden, individueller Sortierung sowie den
Checkboxen Mitarbeiter / Externe. Rechtsklick auf eine Zeile bietet hier NUR
**Adresse anzeigen** (de: "Adresse anzeigen", en: "Show address", fr: "Afficher l'adresse",
it: "Mostra indirizzo") und springt zur Adressbearbeitung des Clients. Beim Scrollen werden
weitere Zeilen nachgeladen (Fortschrittsbalken im Header).

### Gantt-Fläche (rechts)

- Spalten = 365/366 Tage des gewählten Jahres mit Monats- und Tages-Lineal.
- Wochenenden sind farblich hinterlegt (Samstag und Sonntag mit eigenen Farben), Feiertage
  ebenfalls — offizielle und inoffizielle Feiertage in unterschiedlichen Farben.
- Absenzen erscheinen als Balken in der Farbe ihrer Absenz-Art.
- Interaktionen: Klick selektiert Zeile und Eintrag; ein selektierter Balken lässt sich
  verschieben oder an den Ankern links/rechts in der Länge ziehen. Rechtsklick auf einen
  Eintrag öffnet das Kontextmenü der Fläche mit Kopieren/Ausschneiden/Einfügen/Löschen und
  **Umwandeln...** (de: "Umwandeln...", en: "Convert...", fr: "Convertir...", it:
  "Converti...") — Umwandeln ist ein UNTERMENÜ, das alle verfügbaren Absenz-Arten zur
  Auswahl auflistet und die Art des Eintrags direkt wechselt.
- Die Trennlinie zwischen Gantt-Fläche (oben) und Detail-Maske (unten) ist verschiebbar.

### Detail-Maske (unten)

Erscheint, sobald eine Zeile selektiert ist, mit zwei Tabs:

- **Maske** (de: "Maske", en: "Mask", fr: "Masque", it: "Maschera"): bearbeitet den
  selektierten Eintrag — **Von** (de: "Von", en: "From") und **Bis** (de: "Bis", en:
  "Until") als Datums-Picker, **Typ** (Auswahl der Absenz-Art) und **Notizen**
  (Rich-Text-Editor).
- **Liste** (de: "Liste", en: "List", fr: "Liste", it: "Lista"): alle Einträge der
  selektierten Person als Tabelle mit den Spalten **VON**, **BIS**, **ABSENZ**, **WERT**
  und **NOTIZ** (sortierbar; Klick auf eine Zeile selektiert den Eintrag im Diagramm).
  Eigener PDF-Export-Button für diese Liste. **WERT** berechnet der Kalender zur Anzeige
  als Anzahl Tage × Standardwert (defaultValue) der Absenz-Art, nur wenn dieser > 0 ist.

Fußzeile: **+ Neuer Eintrag** (de: "+ Neuer Eintrag", en: "+ New entry") erscheint nur,
wenn eine Mitarbeiterzeile selektiert ist, und legt einen Eintrag an. **Eintrag löschen**
(de: "Eintrag löschen", en: "Delete entry") entfernt den selektierten Eintrag und ist bei
gebuchten Einträgen deaktiviert. Rechts davon blättert eine eigenständige Seitennavigation
(mit Summen-Anzeige) durch die Einträge der Person.

Hinweis: Der Kalender zeigt zwei Arten von Einträgen. **Vorgeplante Absenzen** (hier
erfasste Platzhalter — Erinnerungsstützen für die Planung) sind frei bearbeitbar.
**Gebuchte Absenz-Dienste** aus der Einsatzplanung sind hier schreibgeschützt — die Felder
sind deaktiviert und in der Liste steht **Gebucht** (de: "Gebucht", en: "Booked", fr:
"Réservé", it: "Prenotato") in der Notiz-Spalte.

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenbasis**: Jeder Balken ist entweder eine **vorgeplante Absenz** (hier erfasster
  Platzhalter — eine Erinnerungsstütze für die Planung, frei bearbeitbar) oder ein aus der
  Einsatzplanung **gebuchter Absenz-Dienst** (schreibgeschützt, "Gebucht"). Die Absenz-ART
  (Name, Kürzel, Farbe, Standardlänge, Standardwert, Sichtbarkeit) ist ein Stammdatensatz
  aus den **Einstellungen** (Bereich "Absenzen"). Änderungen dort wirken sofort auf die
  Chips der Legende, die Balkenfarben und neue Einträge; eine Art mit aktivem "im Gantt
  verstecken" verschwindet komplett aus Legende und Diagramm.
- **Einsatzplanung (`/workplace/schedule`)**: Hier erfasste Absenzen erzeugen NUR
  vorgeplante Platzhalter — sie blockieren den Schichtplan nicht automatisch, sondern
  dienen dem Planer als Erinnerungsstütze. Verbindlich wird eine Absenz erst, wenn sie in
  der Einsatzplanung als Absenz-Dienst gebucht wird; solche gebuchten Einträge erscheinen
  hier schreibgeschützt als **Gebucht** und lassen sich nur im Schichtplan ändern.
- **Periodenabschluss (`/workplace/period-closing`)**: In versiegelten Zeiträumen lehnt das
  Backend Neuanlage und Änderung von Absenzen ab — erst entsiegeln, dann korrigieren.
- **Szenarien**: Analyse-Szenarien des Planungs-Assistenten sind isoliert — dort
  vorgeschlagene Absenzen erscheinen nicht im echten Kalender, bis das Szenario übernommen
  wird.
- **Mitgliedschaft**: Der Kalender validiert Einträge gegen die Mitgliedschaftsperiode des
  Mitarbeiters und warnt bei Einträgen vor Beginn, nach Ende oder ausserhalb der Periode.
- **Gruppen-Scope**: Die globale Gruppen-Auswahl in der Kopfleiste wirkt seitenübergreifend —
  wer hier die Gruppe wechselt, sieht auch im Schichtplan, in der Verfügbarkeit und auf den
  anderen Arbeitsbereich-Seiten nur noch diese Gruppe (und umgekehrt). „Fehlende" Mitarbeiter
  im Kalender sind oft nur durch Gruppen-Auswahl oder Suche ausgefiltert.
- **Dashboard**: Sowohl die vorgeplanten Absenzen (Platzhalter) als auch die gebuchten
  Absenz-Dienste erscheinen in den grauen Absenz-Balken des Ressourcen-Monitors. Absenzen
  früh erfassen verbessert die Kapazitätssicht und die Vorschläge des Planungs-Assistenten.
- Die Flags "inkl. Feiertag/Samstag/Sonntag" der Absenz-Art sind derzeit reine Stammdaten
  ohne eigene Berechnungswirkung im Kalender.

### Typische Aufgaben

- Kalender auf eine Person eingrenzen ("zeige die Absenzen von Anna") — Skill
  `search_in_list` (entityType "absence", searchQuery = Name)
- Kalender auf eine Gruppe eingrenzen oder wieder alle zeigen — Skill `select_group`
  (Gruppen-Name oder "all")
- Ferien/Krankheit für einen Mitarbeiter eintragen — Skill `add_break`
- Wer ist wann abwesend? — Skill `search_client_absences`
- Welche Absenz-Arten gibt es (Farbe, Standardlänge)? — Skill `list_absence_types`
- Eintrag wieder entfernen — Skill `delete_break`
- Ausfall kompensieren (Absenz + Ersatzvorschlag als Szenario) — Skill `cover_absence`
- Neue Absenz-Art anlegen/ändern (Stammdaten) — Skills `create_absence` / `update_absence`
- Zur Seite springen — Skill `navigate_to` (Ziel "absences")

### Verwandte Seiten

- Absenz-Arten werden in den **Einstellungen** gepflegt (Bereich "Absenzen": Name, Kürzel,
  Farbe, Beschreibung, unbezahlt/intern).
- `/workplace/schedule` — Einsatzplanung; dort gebuchte Absenz-Dienste erscheinen hier
  schreibgeschützt.
- Adressverwaltung (`explain_address_management`) — Stammdaten der Mitarbeitenden, erreichbar
  auch per Rechtsklick "Adresse anzeigen".

### Trigger-Phrasen

- "Was sehe ich auf dem Absenzen Kalender?"
- "Wie trage ich Ferien für einen Mitarbeiter ein?"
- "Was bedeuten die Farben in diesem Diagramm?"
- "Warum kann ich diesen Eintrag nicht löschen?" (Gebucht/Schedule)
- "What do the columns FROM/UNTIL/VALUE mean on the absence page?"
