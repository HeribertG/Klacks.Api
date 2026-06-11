---
name: explain_page_availability
description: |
  Explains the Availability page (Verfügbarkeit) at /workplace/client-availability — a
  per-employee day/hour checkbox grid where planners record when each employee is
  available. Covers the period navigation (week / 2 weeks / month depending on the
  selected group's payment interval), the hour-grouping slider (1h / 2h / 4h / AM-PM /
  full day), the employee row header with sort/filter popup and context menu, the colour
  coding for weekends and holidays, mouse and keyboard editing and the automatic
  debounced bulk saving. Use this when the user asks what they see on the Availability
  page, what the checkboxes/columns/colours mean, or how to work with it. Supports a
  level parameter: short (purpose only), elements (every element explained), effects
  (data sources, opt-in semantics and how the page interacts with the schedule,
  replacement search, groups and settings).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - availability
  - verfügbarkeit
  - client availability
  - verfügbarkeitsraster
  - availability grid
  - availability page
  - verfügbarkeitsseite
  - verfügbar
  - nicht verfügbar
  - unavailable
  - stundenraster
  - häkchen
  - checkbox
  - häkchen setzen
  - stunden-slider
  - hour grouping
  - stundeneinteilung
  - granularität
  - vm/nm
  - am/pm
  - halbtag
  - ganztag
  - ganztägig
synonyms:
  de: [verfügbarkeit, verfügbarkeitsseite, was sehe ich hier, erkläre diese seite, was bedeutet dieses raster, verfügbarkeitsraster, häkchen setzen, was bedeuten die häkchen, wann ist jemand verfügbar, verfügbarkeit erfassen, halbtags verfügbar, ganztägig verfügbar, stunden-slider, warum ist dieser tag farbig]
  en: [availability, availability page, what do i see here, explain this page, what does this grid mean, availability grid, mark available, set availability, what do the checkboxes mean, half-day availability, full-day availability, hour grouping slider]
  fr: [disponibilité, page de disponibilité, que vois-je ici, explique cette page, que signifie cette grille, grille de disponibilité, saisir la disponibilité, que signifient les cases à cocher, disponibilité demi-journée]
  it: [disponibilità, pagina di disponibilità, cosa vedo qui, spiega questa pagina, cosa significa questa griglia, griglia di disponibilità, registrare la disponibilità, cosa significano le caselle, disponibilità mezza giornata]
---

# Verfügbarkeit — die Seite /workplace/client-availability

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Die Seite **Verfügbarkeit** (de: "Verfügbarkeit", en: "Availability", fr: "Disponibilité",
it: "Disponibilità") zeigt pro Mitarbeiter ein Checkbox-Raster über Tage und Stunden, in
dem per Klick erfasst wird, wann jemand verfügbar ist. Die Periodenlänge (Woche, 2 Wochen
oder Monat) richtet sich nach dem Zahlungsintervall der gewählten Gruppe, die Feinheit der
Stundenspalten nach einem Schieberegler (1h bis ganzer Tag). Es gibt keinen
Speichern-Button — Änderungen werden automatisch gespeichert. Erreichbar über das
Navi-Icon `open-availability` (Tastenkürzel **Alt+3**) oder die Route
`/workplace/client-availability`.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand, wirkt auf diese Seite)

- **Suche** (Suchfeld in der Kopfleiste): filtert auf dieser Seite die Mitarbeiterzeilen
  des Rasters nach **Name oder ID-Nummer** (rein numerische Eingabe = ID-Suche; mehrere
  IDs mit Semikolon möglich). Der Begriff wird als Suchfilter an die Mitarbeiter-Abfrage
  übergeben und die Zeilenliste neu geladen; Leeren der Suche zeigt wieder alle.
- **Gruppen-Auswahl** (Dropdown mit Gruppenbaum, Option **Alle Gruppen** (de: "Alle
  Gruppen", en: "All groups", fr: "Tous les groupes", it: "Tutti i gruppi")): begrenzt
  das Raster auf die Mitglieder der gewählten Gruppe und bestimmt zusätzlich die
  **Periodenlänge** (Zahlungsintervall der Gruppe) sowie den **Feiertagskalender**
  (Kalender-Auswahl der Gruppe). Die Wahl ist global und gilt gleichzeitig für alle
  Arbeitsbereich-Seiten.

### Kopfzeile mit Periodennavigation (oben links)

- **Pfeil-Buttons** links/rechts blättern eine Periode zurück/vor.
- Dazwischen ein **Perioden-Button** (`dropdownSetting`) mit der aktuellen Periode +
  Jahr; ein Klick öffnet einen Mini-Kalender (Wochen-, Zweiwochen- oder Monatsvariante)
  zum direkten Springen.
- Die Periodenlänge richtet sich nach dem Zahlungsintervall der in der Kopfleiste
  gewählten Gruppe (Fallback: Arbeits-Settings): **Woche** (de: "Woche", en: "Week",
  fr: "Semaine", it: "Settimana") = 7 Tage, **2 Wochen** (de: "2 Wochen", en: "2 Weeks",
  fr: "2 semaines", it: "2 settimane") = 14 Tage, **Monat** (de: "Monat", en: "Month",
  fr: "Mois", it: "Mese") = Monatsansicht ab Monatsbeginn.
- Bei Wochenansicht steht vor der Wochennummer **KW** (de: "KW", en: "CW", fr: "Sem.",
  it: "Sett."), bei 2 Wochen z.B. "KW 23-24", bei Monat der Monatsname.

### Raster-Slider (Stunden-Gruppierung, oben rechts)

Ein Schieberegler mit 5 Stufen bestimmt, wie fein die Spalten pro Tag unterteilt sind:

- **1h** / **2h** / **4h** — 24, 12 bzw. 6 Spalten pro Tag.
- **VM/NM** (de: "VM/NM", en: "AM/PM", fr: "Matin/AM", it: "Matt./Pom.") — 2
  Halbtags-Spalten.
- **Tag** (de: "Tag", en: "Day", fr: "Jour", it: "Giorno") — 1 Spalte pro Tag.

Ein Klick auf eine gröbere Zelle setzt immer alle darunterliegenden Stunden gemeinsam;
eine gröbere Zelle zeigt ihr Häkchen nur, wenn ALLE darunterliegenden Stunden als
verfügbar erfasst sind.

### Linke Spalte — Mitarbeiterzeilen

- Pro Zeile der Anzeigename eines Mitarbeiters (eigene + externe Mitarbeiter, keine
  Kunden); die Spaltenbreite ist über den Splitter zwischen Zeilenkopf und Raster
  verschiebbar.
- Welche Personen erscheinen, steuern die globale **Gruppen-Auswahl** und das
  **Suchfeld**; zusätzlich braucht eine Person eine Mitgliedschaft, die den angezeigten
  Zeitraum überlappt.
- Das Filter-Symbol im Zeilenkopf öffnet ein Popup mit Sortierung nach Vorname / Name /
  Firma / **Stunden** (de: "Vertraglich garantierte Stunden", en: "Contractually
  guaranteed hours", fr: "Heures contractuelles garanties", it: "Ore contrattuali
  garantite"), der Option **Individuell Sortiert** (de: "Individuell Sortiert", en:
  "Individual Sort", fr: "Tri individuel", it: "Ordinamento individuale") sowie den
  Checkboxen **Mitarbeiter** (de: "Mitarbeiter", en: "Employees", fr: "Employés", it:
  "Dipendenti") und **Extern** (de: "Extern", en: "Ext", fr: "Ext", it: "Est") zum Ein-/
  Ausblenden der beiden Typen.
- **Rechtsklick** auf eine Zeile öffnet ein Kontextmenü mit NUR **Adresse anzeigen**
  (de: "Adresse anzeigen", en: "Show address", fr: "Afficher l'adresse", it: "Mostra
  indirizzo") und springt zur Adressbearbeitung (`/workplace/edit-address/...`, mit
  Rücksprung zur Verfügbarkeits-Seite).

### Das Verfügbarkeits-Raster (Canvas, rechts)

- Zwei Kopfzeilen: oben der Tag (Wochentag + Datum, z.B. "Mo 5.06"; in der Monatsansicht
  ohne Monatszahl, z.B. "Mo 5."), darunter die Stunden-Slots (bei 1h die Startstunde
  "8", bei 2h "8-9", bei 4h "8-11", bei Halbtagen **VM** (de: "VM", en: "AM", fr:
  "Matin", it: "Matt.") / **NM** (de: "NM", en: "PM", fr: "AM", it: "Pom.")). In der
  Tagesansicht steht oben nur der Wochentag und unten der Monatstag.
- Jede Zelle enthält eine **Checkbox**: Häkchen = für diese Stunden als verfügbar
  erfasst.
- Farbcodierung der Tageshintergründe: Samstag und Sonntag haben eigene
  Hintergrundfarben, Feiertag und offizieller Feiertag ebenfalls (alle aus den
  Grid-Farbeinstellungen; Vorrang: offizieller Feiertag vor Feiertag vor Sonntag vor
  Samstag). Ungerade Slots sind leicht abgedunkelt (Zebra-Muster). Der Feiertagskalender
  kommt aus der Kalender-Auswahl der gewählten Gruppe, sonst aus der globalen
  Einstellung.
- Horizontale und vertikale Scrollbalken erscheinen nur, wenn der Inhalt grösser als der
  sichtbare Ausschnitt ist; das Mausrad scrollt ebenfalls.

### Bedienung und Speichern

- **Klick** schaltet eine Zelle um; **Ziehen mit gedrückter Maustaste** selektiert bzw.
  deselektiert weitere Zellen und Zeilen — je nachdem, ob die erste Zelle gesetzt oder
  entfernt wurde (am Rand scrollt das Raster automatisch mit).
- **Ctrl+Klick/Ziehen** entfernt immer das Häkchen.
- **Tastatur** (Raster zuerst fokussieren, es ist per Tab erreichbar): Pfeiltasten
  bewegen die Auswahl (auch Tab = rechts, Backspace = links), PageUp/PageDown blättern
  seitenweise durch die Zeilen, Home/End springen zur ersten/letzten Zeile,
  **Leertaste/Enter** schaltet die Zelle um.
- Es gibt **keinen Speichern-Button**: Änderungen werden gebündelt und ~0,8 s nach der
  letzten Änderung automatisch als Bulk-Upsert gespeichert (ein Datensatz pro
  Mitarbeiter, Datum und Stunde). Schlägt das Speichern fehl, werden die offenen
  Änderungen automatisch erneut versucht.

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenbasis**: Jede Stunde ist ein eigener Datensatz (Mitarbeiter, Datum, Stunde,
  verfügbar ja/nein). Das Speichern ist ein Upsert auf genau diesen Schlüssel —
  erneutes Setzen überschreibt den alten Wert.
- **Opt-in und spärlich**: Eine nie angeklickte Zelle hat KEINEN Datensatz und bedeutet
  "keine Einschränkung". Häkchen setzen speichert "explizit verfügbar"; ein gesetztes
  Häkchen wieder entfernen speichert "explizit NICHT verfügbar" — im Raster sehen beide
  Zustände (kein Datensatz / explizit nicht verfügbar) gleich aus (leere Checkbox).
- **Ersatzsuche (`find_replacement`)**: Nur eine explizit als nicht verfügbar
  gespeicherte Stunde blockiert einen Kandidaten, wenn sie das Zeitfenster der Schicht
  überlappt; fehlende Einträge schliessen niemanden aus. Bei Schichten über Mitternacht
  wird nur der Anteil am Starttag geprüft (dokumentierte v1-Grenze).
- **Einsatzplanung (`/workplace/schedule`)**: Die explizit verfügbaren Stunden werden zu
  zusammenhängenden Zeitbereichen gruppiert und in den Zellen des Schichtplans als
  hinterlegte Verfügbarkeits-Bereiche eingezeichnet — der Planer sieht dort direkt, wann
  jemand verfügbar gemeldet ist.
- **Wer erscheint**: Kunden nie; eigene und externe Mitarbeiter nur mit einer
  Mitgliedschaft, die den angezeigten Zeitraum überlappt (Beginn vor Periodenende, Ende
  nach Periodenbeginn oder offen). Die Checkboxen Mitarbeiter/Extern im Filter-Popup
  schränken zusätzlich ein.
- **Gruppen-Scope**: Die globale Gruppen-Auswahl wirkt seitenübergreifend. Auf dieser
  Seite löst ein Gruppenwechsel zusätzlich aus: Periodenlänge wird neu aus dem
  Zahlungsintervall der Gruppe bestimmt, Startdatum zurückgesetzt, Feiertagskalender der
  Gruppe geladen und Mitarbeiterliste + Verfügbarkeiten neu geladen. „Fehlende"
  Mitarbeiter sind oft nur durch Gruppen-Auswahl oder Suche ausgefiltert.
- **Einstellungen**: Die Hintergrundfarben für Wochenende/Feiertage kommen aus den
  Grid-Farbeinstellungen; das Fallback-Zahlungsintervall (wenn keine Gruppe gewählt ist)
  aus den Arbeits-Settings; der Fallback-Feiertagskalender aus der globalen
  Kalender-Auswahl.
- **Szenarien & Periodenabschluss**: Verfügbarkeiten kennen kein Analyse-Szenario —
  dieselben Einträge gelten im Original- wie im Szenario-Modus. Ein Bezug zum
  Periodenabschluss/DayLock besteht nicht; Verfügbarkeiten bleiben auch in versiegelten
  Zeiträumen änderbar.
- **Berechtigungen**: Verfügbarkeiten lesen erfordert das Recht, Mitarbeiterdaten zu
  sehen (CanViewClients); per Chat setzen erfordert Bearbeitungsrecht (CanEditClients).

### Typische Aufgaben

- Verfügbarkeit eines Mitarbeiters per Chat erfassen — Skill `set_client_availability`
  (einzelne Tage als Liste oder Zeitraum bis 92 Tage, optionales Stundenfenster,
  Standard = ganzer Tag; clientId vorher z.B. via `search_employees` auflösen)
- Verfügbarkeiten eines Zeitraums abfragen — Skill `list_client_availabilities`
- Ersatz unter Berücksichtigung der Verfügbarkeit suchen — Skill `find_replacement`
- Raster auf eine Gruppe eingrenzen oder wieder alle zeigen — Skill `select_group`
  (Gruppen-Name oder "all")
- Zur Seite springen — Skill `navigate_to` (Ziel "client-availability")
- Einzelne Person finden: Suchfeld der Kopfleiste verwenden (Name oder ID-Nummer)

### Verwandte Seiten

- **Einsatzplanung** (`/workplace/schedule`) — zeigt die erfassten
  Verfügbarkeits-Bereiche in den Zellen; die Ersatzsuche respektiert explizite
  Nicht-Verfügbarkeit.
- **Adressverwaltung** (`explain_address_management`) — Stammdaten der Mitarbeitenden,
  per Rechtsklick "Adresse anzeigen" direkt erreichbar.
- **Gruppen-Verwaltung** — das Zahlungsintervall und die Kalender-Auswahl der Gruppe
  steuern Periodenlänge und Feiertagsfarben dieser Seite.
- **Einstellungen** — Grid-Farben (Wochenende/Feiertage), Arbeits-Settings
  (Fallback-Zahlungsintervall) und globale Kalender-Auswahl.

### Trigger-Phrasen

- "Was sehe ich auf der Verfügbarkeits-Seite?"
- "Was bedeuten die Häkchen in diesem Raster?"
- "Wie erfasse ich, wann ein Mitarbeiter verfügbar ist?"
- "Warum ist dieser Tag farbig hinterlegt?" (Wochenende/Feiertag)
- "What does the availability grid show and how do I edit it?"
- "À quoi sert la page de disponibilité ?"
