---
name: explain_page_availability
description: |
  Explains the Availability page (/workplace/client-availability): a per-employee day/hour
  checkbox grid where planners record when each employee is available. Covers the period
  navigation (week / 2 weeks / month depending on the group's payment interval), the
  hour-grouping slider (1h / 2h / 4h / AM-PM / full day), the employee row header with
  filter and context menu, the colour coding for weekends and holidays, mouse/keyboard
  editing and the automatic debounced saving. Use this when the user asks what they see
  on the Availability page, what the cards/charts/columns mean, or how to work with it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - availability
  - verfügbarkeit
  - client availability
  - verfügbarkeitsraster
  - availability grid
  - verfügbar
  - nicht verfügbar
  - unavailable
  - stundenraster
synonyms:
  de: [verfügbarkeit, verfügbarkeitsseite, was sehe ich hier, erkläre diese seite, was bedeutet dieses raster, verfügbarkeitsraster, häkchen setzen, wann ist jemand verfügbar]
  en: [availability, availability page, what do i see here, explain this page, what does this grid mean, availability grid, mark available, set availability]
  fr: [disponibilité, page de disponibilité, que vois-je ici, explique cette page, que signifie cette grille, grille de disponibilité]
  it: [disponibilità, pagina di disponibilità, cosa vedo qui, spiega questa pagina, cosa significa questa griglia, griglia di disponibilità]
---

# Verfügbarkeit — das Tag/Stunden-Raster pro Mitarbeiter

## Kern-Idee (1 Satz)

Die Seite **Verfügbarkeit** (de: "Verfügbarkeit", en: "Availability", fr: "Disponibilité",
it: "Disponibilità") unter `/workplace/client-availability` zeigt pro Mitarbeiter ein
Checkbox-Raster über Tage und Stunden, in dem per Klick erfasst wird, wann jemand
verfügbar ist — erreichbar über das Navi-Icon `open-availability` (Tooltip
"Verfügbarkeit", Tastenkürzel **Alt+3**).

## Bereiche der Seite (in Render-Reihenfolge)

### 1. Kopfzeile mit Periodennavigation

- **Pfeil-Buttons** links/rechts blättern eine Periode zurück/vor.
- Dazwischen ein **Perioden-Button** mit der aktuellen Periode + Jahr; ein Klick öffnet
  einen Mini-Kalender zum direkten Springen.
- Die Periodenlänge richtet sich nach dem Zahlungsintervall der in der Navi gewählten
  Gruppe (Fallback: Arbeits-Settings): **Woche** (de: "Woche", en: "Week", fr: "Semaine",
  it: "Settimana") = 7 Tage, **2 Wochen** (de: "2 Wochen", en: "2 Weeks", fr: "2 semaines",
  it: "2 settimane") = 14 Tage, **Monat** (de: "Monat", en: "Month", fr: "Mois",
  it: "Mese") = Monatsansicht ab Monatsbeginn.
- Bei Wochenansicht steht vor der Wochennummer **KW** (de: "KW", en: "CW", fr: "Sem.",
  it: "Sett."), bei 2 Wochen z.B. "KW 23-24", bei Monat der Monatsname.

### 2. Raster-Slider (Stunden-Gruppierung)

Rechts in der Kopfzeile sitzt ein Schieberegler mit 5 Stufen, der bestimmt, wie fein die
Spalten pro Tag unterteilt sind:

- **1h** / **2h** / **4h** — 24, 12 bzw. 6 Spalten pro Tag.
- **VM/NM** (de: "VM/NM", en: "AM/PM", fr: "Matin/AM", it: "Matt./Pom.") — 2 Halbtags-Spalten.
- **Tag** (de: "Tag", en: "Day", fr: "Jour", it: "Giorno") — 1 Spalte pro Tag.

Ein Klick auf eine gröbere Zelle setzt immer alle darunterliegenden Stunden gemeinsam.

### 3. Linke Spalte — Mitarbeiterzeilen

- Zeigt pro Zeile den Anzeigenamen des Mitarbeiters (eigene + externe Mitarbeiter);
  die Spaltenbreite ist über den Splitter verschiebbar.
- Welche Personen erscheinen, steuern die in der Navi gewählte **Gruppe** und das globale
  Suchfeld (Namenssuche).
- Ein Klick auf das Filter-Symbol im Zeilenkopf öffnet ein Filterfenster (Sortierung,
  eigene/externe Mitarbeiter ein-/ausblenden, individuelle Sortierreihenfolge).
- **Rechtsklick** auf eine Zeile öffnet ein Kontextmenü, das direkt zur Adresse des
  Mitarbeiters führt (`/workplace/edit-address/...`).

### 4. Das Verfügbarkeits-Raster (Canvas)

- Zwei Kopfzeilen: oben der Tag (Wochentag + Datum, z.B. "Mo 5.06"), darunter die
  Stunden-Slots (z.B. "8-9" bei 2h, **VM**/**NM** bei Halbtagen; in der Tagesansicht
  steht unten der Monatstag).
- Jede Zelle enthält eine **Checkbox**: Häkchen = für diese Stunden als verfügbar erfasst.
- Farbcodierung der Tageshintergründe: Samstag, Sonntag, Feiertag und offizieller
  Feiertag haben eigene Hintergrundfarben (aus den Grid-Farbeinstellungen); ungerade
  Slots sind leicht abgedunkelt (Zebra-Muster). Der Feiertagskalender kommt aus der
  Kalender-Auswahl der Gruppe bzw. der globalen Einstellung.

## Bedienung und Speichern

- **Klick** schaltet eine Zelle um; **Ziehen mit gedrückter Maustaste** malt denselben
  Wert über mehrere Zellen/Zeilen (am Rand scrollt das Raster automatisch mit).
- **Ctrl+Klick/Ziehen** entfernt immer (setzt auf nicht verfügbar).
- **Tastatur** (Raster fokussieren): Pfeiltasten/Tab bewegen die Auswahl, PageUp/PageDown
  blättern, Home/End springen zur ersten/letzten Zeile, **Leertaste/Enter** schaltet die
  Zelle um. Mausrad scrollt.
- Es gibt **keinen Speichern-Button**: Änderungen werden automatisch gebündelt und
  ~0,8 s nach der letzten Änderung als Bulk-Upsert gespeichert (pro Mitarbeiter, Datum
  und Stunde); bei Fehlern wird der Speicherversuch wiederholt.

## Wirkung auf die Planung

Verfügbarkeit ist **opt-in und spärlich**: Ein fehlender Eintrag bedeutet "keine
Einschränkung". Nur eine explizit als nicht verfügbar gespeicherte Stunde blockiert den
Mitarbeiter — z.B. schliesst die Ersatzsuche (`find_replacement`) Kandidaten aus, deren
Nicht-Verfügbarkeit das Zeitfenster der Schicht überlappt. Die erfassten Zeiten fliessen
ausserdem in die Dienstplan-Daten (Schedule-Entries) ein.

## Typische Aufgaben auf dieser Seite

- Verfügbare/abwesende Zeiten eines Mitarbeiters für die nächste Periode erfassen.
- Halbtags- oder Tagesverfügbarkeit schnell über den Raster-Slider setzen.
- Vor der Planung prüfen, wer an einem Tag/Slot überhaupt einsetzbar ist.
- Per Chat: `set_client_availability` (Verfügbarkeit pro Tag/Stunde setzen),
  `list_client_availabilities` (Einträge eines Zeitraums abfragen),
  `find_replacement` (Ersatz unter Berücksichtigung der Verfügbarkeit suchen),
  `navigate_to` mit Seite `client-availability` (Seite öffnen).

## Verwandte Seiten

- **Dienstplan** (`/workplace/schedule`) — dort wirkt sich die erfasste Verfügbarkeit aus.
- **Adressverwaltung** (`explain_address_management`) — Stammdaten der Mitarbeiter;
  per Rechtsklick direkt erreichbar.

## Trigger-Phrasen

- "Was sehe ich auf der Verfügbarkeits-Seite?"
- "Was bedeuten die Häkchen in diesem Raster?"
- "Wie erfasse ich, wann ein Mitarbeiter verfügbar ist?"
- "What does the availability grid show and how do I edit it?"
- "À quoi sert la page de disponibilité ?"
