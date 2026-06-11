---
name: explain_page_dashboard
description: |
  Explains the Klacks dashboard page (/workplace/dashboard) — the start page reached via the
  header logo. Describes its four reorderable sections: Overview (two donut charts: customers
  per group and shifts per group), Coverage & Confirmation (two donut charts for the current
  month: covered shift slots per group and confirmed/sealed work entries per group), Resources
  (Resource Monitor — a 365-day stacked bar chart with services/absences bars and three
  reference lines for desired readiness, max readiness and total employees, plus year/group/zoom
  controls) and Locations (Leaflet map with clustered client markers, address search, map styles
  and per-city or per-group location cards). Use this when the user asks what they see on the
  dashboard page, what the cards/charts/columns mean, or how to work with it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - dashboard
  - übersicht
  - overview
  - startseite
  - tortendiagramm
  - pie chart
  - ressourcen-monitor
  - resource monitor
  - standorte
  - locations
synonyms:
  de: [dashboard, übersicht, startseite, was sehe ich hier, erkläre diese seite, was bedeutet dieses diagramm, was bedeutet diese karte, ressourcen-monitor]
  en: [dashboard, overview, home page, what do i see here, explain this page, what does this chart mean, what does this card mean, resource monitor]
  fr: [tableau de bord, aperçu, que vois-je ici, explique cette page, que signifie ce graphique, que signifie cette carte, moniteur de ressources]
  it: [dashboard, panoramica, cosa vedo qui, spiega questa pagina, cosa significa questo grafico, cosa significa questa scheda, monitor risorse]
---

# Dashboard — die Startseite von Klacks

## Zweck + Weg dorthin (1 Satz)

Das Dashboard unter `/workplace/dashboard` zeigt die wichtigsten Kennzahlen des Betriebs auf
einen Blick — erreichbar per Klick auf das Logo links oben im Header (`header-logo-icon`,
bzw. `header-logo-image` bei eigenem Firmenlogo).

## Aufbau

Vier Abschnitte, jeder einzeln auf-/zuklappbar (Klick auf die Abschnitts-Zeile). Oben rechts:
Alle aufklappen (`dashboard-expand-all-btn`), alle zuklappen (`dashboard-collapse-all-btn`) und
das Abschnitts-Menü (`dashboard-section-menu-btn`, Titel **Abschnitte** (de: "Abschnitte",
en: "Sections", fr: "Sections", it: "Sezioni")) zum Ein-/Ausblenden einzelner Abschnitte.
Abschnitte lassen sich am Griff-Symbol per Drag&Drop umsortieren; Sichtbarkeit und Reihenfolge
werden im Browser (localStorage) gemerkt. Ohne Berechtigung ist nur der Abschnitt "Standorte" sichtbar.

## Abschnitt 1: **Übersicht** (de: "Übersicht", en: "Overview", fr: "Aperçu", it: "Panoramica")

Zwei Donut-Diagramme nebeneinander; Datenquelle ist der Gruppenbaum (`Groups/tree`), inklusive
aller Untergruppen. Jedes Tortenstück = eine Gruppe (eigene Farbe); in der Mitte steht die
Gesamtzahl ("Gesamt"), daneben eine Legende mit Gruppenname, Wert und Prozent. Hover hebt das
Stück hervor und zeigt einen Tooltip "Gruppe: Wert (Prozent)".

- **Kunden nach Gruppen** (de: "Kunden nach Gruppen", en: "Customers by Groups",
  fr: "Clients par groupes", it: "Clienti per gruppi"): wie viele Kunden jede Gruppe hat
  (nur Gruppen mit mindestens 1 Kunde).
- **Dienste nach Gruppen** (de: "Dienste nach Gruppen", en: "Shifts by Groups",
  fr: "Services par groupes", it: "Turni per gruppi"): wie viele Dienste (Shifts) jeder
  Gruppe zugeordnet sind.

## Abschnitt 2: **Abdeckung & Bestätigung** (de: "Abdeckung & Bestätigung", en: "Coverage & Confirmation", fr: "Couverture & Confirmation", it: "Copertura & Conferma")

Zwei Donut-Diagramme; Datenquelle `Dashboard/ShiftCoverageStatistics`
(`GetShiftCoverageStatisticsQuery`). Zeitraum: immer der **laufende Kalendermonat**, begrenzt
auf die für den Nutzer sichtbaren Gruppen.

- **Abdeckung nach Gruppen** (de: "Abdeckung nach Gruppen", en: "Coverage by Groups",
  fr: "Couverture par groupes", it: "Copertura per gruppi"): pro Gruppe die Anzahl
  **besetzter Dienst-Slots** im Monat (gebuchte von den geplanten Plätzen).
- **Bestätigt nach Gruppen** (de: "Bestätigt nach Gruppen", en: "Confirmed by Groups",
  fr: "Confirmé par groupes", it: "Confermato per gruppi"): pro Gruppe die Anzahl
  **bestätigter Einsätze** (Work-Einträge mit Sperrstufe "Confirmed" oder höher, d.h. versiegelt).

## Abschnitt 3: **Ressourcen** (de: "Ressourcen", en: "Resources", fr: "Ressources", it: "Risorse")

Eine Karte mit Titel **Ressourcen-Monitor** (de: "Ressourcen-Monitor", en: "Resource Monitor",
fr: "Moniteur de ressources", it: "Monitor risorse"); Datenquelle `Dashboard/ResourceMonitor`
(`GetResourceMonitorQuery`): 365 Tageswerte des gewählten Jahres. Zwei Tabs: **Diagramm**
(de: "Diagramm", en: "Chart", fr: "Graphique", it: "Grafico") und **Erklärung** (de: "Erklärung",
en: "Manual", fr: "Manuel", it: "Manuale") mit eingebauter Anleitung. Bedienelemente: Zoom-Slider
(100–400 %, streckt das Diagramm horizontal), Jahres-Zähler und Gruppen-Auswahl (Filter).

So liest man das Diagramm (Einheit "MA" = Mitarbeiter, pro Tag ein gestapelter Balken):

- **Grüner Balken** = **Dienste** (de: "Dienste", en: "Services", fr: "Services", it: "Servizi"):
  Anzahl an dem Tag geplanter Dienste (je Dienst 1 Position).
- **Grauer Balken obendrauf** = **Absenzen** (de: "Absenzen", en: "Absences", fr: "Absences",
  it: "Assenze"): Summe der Absenz-Werte (Ferien, Krankheit usw.) an dem Tag.
- **Rosa gepunktete Linie** = **Wunsch-Einsatzbereitschaft** (de: "Wunsch-Einsatzbereitschaft",
  en: "Desired Readiness", fr: "Disponibilité souhaitée", it: "Disponibilità desiderata"):
  gewünschte tägliche Verfügbarkeit gemäss Vertrags-Wochentagsmuster der Mitarbeiter.
- **Rote gestrichelte Linie** = **Max. Einsatzbereitschaft** (de: "Max. Einsatzbereitschaft",
  en: "Max. Readiness", fr: "Disponibilité max.", it: "Disponibilità max."): Obergrenze der
  Verfügbarkeit (mit Limit max. aufeinanderfolgender Arbeitstage).
- **Blaue durchgezogene Linie** = **Anzahl Mitarbeiter** (de: "Anzahl Mitarbeiter",
  en: "Total Employees", fr: "Total des employés", it: "Totale dipendenti"): Gesamtbestand
  aktiver Mitarbeiter an dem Tag.

Samstage, Sonntage und Feiertage sind im Hintergrund farblich markiert (Feiertage mit Tooltip),
Monatsgrenzen sind beschriftet. Liegen die grünen Balken über den Linien, ist mehr geplant als
verfügbar (Unterdeckung); liegen sie deutlich darunter, gibt es freie Kapazität.

## Abschnitt 4: **Standorte** (de: "Standorte", en: "Locations", fr: "Emplacements", it: "Ubicazioni")

Zwei Ansichten, umschaltbar per Buttons **Nach Stadt** (de: "Nach Stadt", en: "By City",
fr: "Par ville", it: "Per città") und **Nach Gruppe** (de: "Nach Gruppe", en: "By Group",
fr: "Par groupe", it: "Per gruppo"); die Wahl wird gemerkt.

- **Nach Stadt**: Datenquelle `Dashboard/ClientLocations` (`GetClientLocationsQuery`) — alle
  aktiven Adressen (Clients) mit gültiger aktueller Adresse, geokodiert und pro Stadt+Land
  gruppiert. Oben ein Adress-Suchfeld (de: "Adresse suchen..."), darunter eine Leaflet-Karte
  mit geclusterten Markern. Marker-Klick öffnet ein Popup mit Total sowie Aufschlüsselung
  Employees / Extern Emp / Customers und einem Street-View-Link. Darunter: **Kartenstil**
  (de: "Kartenstil", en: "Map Style") mit 6 Kartenstilen (OSM Standard, OSM DE, CartoDB
  Positron/Dark/Voyager, OpenTopoMap) und der Button **Alle anzeigen** (de: "Alle anzeigen",
  en: "Show all") zum Heranzoomen auf alle Marker.
- Unter der Karte eine Kachel-Liste pro Standort: Stadt, Land, Badges **MA** (Mitarbeiter),
  **Ext** (externe Mitarbeiter), **KD** (Kunden) und das Total **Adressen**; Klick auf eine
  Kachel zoomt die Karte dorthin, der Street-View-Button öffnet Google Street View.
  Kopfzeile: **Anzahl Standorte** und **Anzahl Adressen**.
- **Nach Gruppe**: dieselben Kacheln, aber pro Gruppe aggregiert (ohne Karte);
  Kopfzeile zeigt **Anzahl Gruppen** statt Standorte.

## Typische Aufgaben + passende Klacksy-Skills

- Schnell-Lagebild des Monats abrufen → `get_dashboard_summary`
- Über-/Unterdeckung im Ressourcen-Monitor deuten lassen → `interpret_resource_monitor`
- Standort-Karte zusammenfassen lassen → `get_client_locations_overview`
- Zum Dashboard oder weiter zu einer Liste springen → `navigate_to`, `search_in_list`
- Gruppen-Koordinaten für geographische Auswertungen pflegen → `set_group_location`, `geocode_location_groups`

## Verwandte Seiten

- Schichtplan (`/workplace/schedule`) — dort entstehen die Dienste/Slots, die Abdeckung &
  Bestätigung und der Ressourcen-Monitor auswerten.
- Mitarbeiterliste (`/workplace/client`) und Gruppen (`/workplace/group`) — Quelle der
  Kunden-/Dienste-Zahlen pro Gruppe und der Adressen auf der Karte.
- Absenzen (`/workplace/absence`) — Quelle der grauen Absenz-Balken.
