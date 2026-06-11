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
  cards/charts/columns mean, or how to work with it.
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
synonyms:
  de: [was sehe ich hier, erkläre diese seite, was bedeutet dieses diagramm, was bedeutet diese karte, absenzen kalender, abwesenheitskalender, absenz eintragen, ferien eintragen]
  en: [what do i see here, explain this page, what does this chart mean, what does this card mean, absence calendar, absence gantt, enter absence, vacation calendar]
  fr: [que vois-je ici, explique cette page, que signifie ce diagramme, calendrier d'absences, saisir une absence, congés, gantt des absences]
  it: [cosa vedo qui, spiega questa pagina, cosa significa questo diagramma, calendario assenze, inserire un'assenza, ferie, gantt delle assenze]
---

# Absenzen Kalender — die Seite /workplace/absence

## Zweck (1 Satz)

Der **Absenzen Kalender** (de: "Absenzen Kalender", en: "Absence Calendar", fr: "Calendrier
d'absences", it: "Calendario assenze") zeigt ein ganzes Kalenderjahr als Gantt-Diagramm —
eine Zeile pro Mitarbeiter, eine Spalte pro Tag — und dient zum Eintragen, Verschieben und
Bearbeiten von Absenzen (Ferien, Krankheit usw.). Erreichbar über das Navi-Icon
`open-absences` (Gantt-Symbol, Alt+2) oder die Route `/workplace/absence`.

## Kopfzeile (oben)

- **Absenz-Arten-Liste**: alle konfigurierten Absenz-Arten als farbige Chips (Farbe = Farbe
  der Balken im Diagramm). Klick auf das Häkchen blendet die Art im Diagramm ein/aus
  (Filter). Ein Chip lässt sich per **Drag & Drop** auf eine Mitarbeiterzeile ziehen — das
  erzeugt direkt einen neuen Eintrag mit der Standardlänge der Art.
- **PDF-Export-Button**: exportiert das Gantt-Diagramm als PDF.
- **Jahr** (de: "Jahr:", en: "Year:", fr: "Année:", it: "Anno:"): Zähler zum Wechseln des
  angezeigten Jahres.
- **Zoom-Slider**: 50–300 %, vergrößert/verkleinert die Tagesspalten.

## Zeilen-Header (links)

Zeigt die Mitarbeitenden (eine Zeile pro Person). Das Filter-Icon öffnet ein Popup mit
Sortierung nach Vorname / Name / Firma / Stunden, individueller Sortierung sowie den
Checkboxen Mitarbeiter / Externe. Rechtsklick auf eine Zeile bietet **Adresse anzeigen**
(de: "Adresse anzeigen", en: "Show address", fr: "Afficher l'adresse", it: "Mostra
indirizzo") und springt zur Adressbearbeitung des Clients. Beim Scrollen werden weitere
Zeilen nachgeladen (Fortschrittsbalken im Header).

## Gantt-Fläche (rechts)

- Spalten = 365/366 Tage des gewählten Jahres mit Monats- und Tages-Lineal.
- Wochenenden sind farblich hinterlegt (Samstag und Sonntag mit eigenen Farben), Feiertage
  ebenfalls — offizielle und inoffizielle Feiertage in unterschiedlichen Farben.
- Absenzen erscheinen als Balken in der Farbe ihrer Absenz-Art.
- Interaktionen: Klick selektiert Zeile und Eintrag; ein selektierter Balken lässt sich
  verschieben oder an den Ankern links/rechts in der Länge ziehen. Rechtsklick auf einen
  Eintrag öffnet das Kontextmenü mit Kopieren/Ausschneiden/Einfügen/Löschen und
  **Umwandeln...** (de: "Umwandeln...", en: "Convert...", fr: "Convertir...", it:
  "Converti...") zum Wechsel der Absenz-Art.

## Detail-Maske (unten)

Erscheint, sobald eine Zeile selektiert ist, mit zwei Tabs:

- **Maske** (de: "Maske", en: "Mask", fr: "Masque", it: "Maschera"): bearbeitet den
  selektierten Eintrag — **Von** (de: "Von", en: "From") und **Bis** (de: "Bis", en:
  "Until") als Datums-Picker, **Typ** (Auswahl der Absenz-Art) und **Notizen**
  (Rich-Text-Editor).
- **Liste** (de: "Liste", en: "List", fr: "Liste", it: "Lista"): alle Einträge der
  selektierten Person als Tabelle mit den Spalten **VON**, **BIS**, **ABSENZ**, **WERT**
  und **NOTIZ** (sortierbar; Klick auf eine Zeile selektiert den Eintrag im Diagramm).
  Eigener PDF-Export-Button für diese Liste. **WERT** = Anzahl Tage × Standardwert
  (defaultValue) der Absenz-Art, nur wenn dieser > 0 ist.

Fußzeile: **+ Neuer Eintrag** (de: "+ Neuer Eintrag", en: "+ New entry") legt einen Eintrag
an, **Eintrag löschen** (de: "Eintrag löschen", en: "Delete entry") entfernt den
selektierten; daneben blättert eine Seitennavigation durch die Einträge der Person.

Hinweis: Absenzen, die aus der Einsatzplanung stammen (gebuchte Absenz-Dienste), sind hier
schreibgeschützt — die Felder sind deaktiviert und in der Liste steht **Gebucht** (de:
"Gebucht", en: "Booked", fr: "Réservé", it: "Prenotato") in der Notiz-Spalte. Die App
validiert zudem, dass eine Absenz innerhalb der Mitgliedschaftsperiode des Mitarbeiters
liegt.

## Typische Aufgaben

- Ferien/Krankheit für einen Mitarbeiter eintragen — Skill `add_break`
- Wer ist wann abwesend? — Skill `search_client_absences`
- Welche Absenz-Arten gibt es (Farbe, Standardlänge)? — Skill `list_absence_types`
- Eintrag wieder entfernen — Skill `delete_break`
- Ausfall kompensieren (Absenz + Ersatzvorschlag als Szenario) — Skill `cover_absence`
- Neue Absenz-Art anlegen/ändern (Stammdaten) — Skills `create_absence` / `update_absence`
- Zur Seite springen — Skill `navigate_to` (Ziel "absences")

## Verwandte Seiten

- Absenz-Arten werden in den **Einstellungen** gepflegt (Bereich "Absenzen": Name, Kürzel,
  Farbe, Beschreibung, unbezahlt/intern).
- `/workplace/schedule` — Einsatzplanung; dort gebuchte Absenz-Dienste erscheinen hier
  schreibgeschützt.
- Adressverwaltung (`explain_address_management`) — Stammdaten der Mitarbeitenden, erreichbar
  auch per Rechtsklick "Adresse anzeigen".

## Trigger-Phrasen

- "Was sehe ich auf dem Absenzen Kalender?"
- "Wie trage ich Ferien für einen Mitarbeiter ein?"
- "Was bedeuten die Farben in diesem Diagramm?"
- "Warum kann ich diesen Eintrag nicht löschen?" (Gebucht/Schedule)
- "What do the columns FROM/UNTIL/VALUE mean on the absence page?"
