---
name: explain_page_schedule
description: |
  Explains the Schedule page (Einsatzplan) at /workplace/schedule — the central planning
  board with an employees-by-days grid. Covers the toolbar (period navigation by week/
  bi-week/month, send email, AutoWizard with Plan/Fuzzy Harmonizer/Holistic Harmonizer,
  schedule PDF export, desired-absence bars, command and availability toggles, hour
  recalculation, scenario selector, day-timeline switch, zoom), the row header with
  sort/filter popup and hour slots, the grid with drag & drop booking, cell context menus,
  lock levels and sealed days, cross-group sealed entries, and the lower shift section
  with the Shifts and Error List tabs. Use this when the user asks what they see on the
  Schedule page, what the grid/toolbar buttons/locks mean, or how to work with it.
  Supports a level parameter: short (purpose only), elements (every element explained),
  effects (data sources, scenarios, period closing/day locks and how the page interacts
  with the absence calendar, shifts, groups and the dashboard).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - schedule
  - schedule page
  - einsatzplan
  - dienstplan
  - schichtplan
  - plantafel
  - roster
  - planung
  - grid
  - szenario
  - scenario
  - wizard
  - autowizard
  - harmonizer
  - fuzzy harmonizer
  - neuberechnen
  - recalculate
  - period hours
  - stunden
  - einsatzplan pdf
  - schedule pdf
  - pdf export
  - versiegelt
  - sealed
  - locked
  - gesperrt
  - bestätigen
  - confirm
  - tagesverlauf
  - day timeline
  - gewünschte absenzen
  - verfügbarkeit
  - availability
  - befehle anzeigen
synonyms:
  de: [was sehe ich hier, erkläre diese seite, was bedeutet dieses raster, was bedeutet diese ansicht, einsatzplan, dienstplan, schichtplan, plantafel, was sind die buttons in der toolbar, kopfleiste, stunden neuberechnen, einsatzplan als pdf, szenario übernehmen, szenario verwerfen, versiegelte tage, gesperrte einträge, warum kann ich nicht bearbeiten, gewünschte absenzen, tagesverlauf, dienst buchen]
  en: [what do i see here, explain this page, what does this grid mean, schedule, schedule page, roster, duty plan, what are the toolbar buttons, recalculate hours, how do i recalculate period hours, export schedule as pdf, accept scenario, reject scenario, sealed days, locked entries, why can't i edit, day timeline, book a shift]
  fr: [que vois-je ici, explique cette page, que signifie cette vue, planning, plan de service, horaire, boutons de la barre d'outils, recalculer les heures, exporter le planning en pdf, accepter le scénario, rejeter le scénario, jours scellés, entrées verrouillées, pourquoi je ne peux pas modifier, déroulé de la journée]
  it: [cosa vedo qui, spiega questa pagina, cosa significa questa vista, pianificazione, piano di servizio, turni, pulsanti della barra degli strumenti, ricalcolare le ore, esportare la pianificazione in pdf, accettare lo scenario, rifiutare lo scenario, giorni sigillati, voci bloccate, perché non posso modificare, andamento giornaliero]
---

# Einsatzplan — die Plantafel (/workplace/schedule)

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Der **Einsatzplan** ist die zentrale Plantafel von Klacks: ein Raster **Mitarbeiter × Tage**,
in dem Schichten per Drag & Drop platziert, geprüft, korrigiert und bestätigt werden.
Erreichbar über die Route `/workplace/schedule` oder das oberste Navi-Icon `open-schedules`
(Tooltip **Alle Planungen** (de: "Alle Planungen", en: "All Schedules", fr: "Tous les
plannings", it: "Tutte le pianificazioni"), Shortcut Alt+1). Welche Mitarbeiterzeilen
sichtbar sind, bestimmen die globale **Gruppen-Auswahl** und die Suche in der Kopfleiste
der App. Neben dem Tabellenraster gibt es eine umschaltbare **Tagesverlauf**-Ansicht
(Timeline pro Tag), und automatische Planungsläufe (Wizard/Harmonizer) schreiben ihre
Ergebnisse als Szenario, das der Planer übernehmen oder verwerfen muss.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand, wirkt auf diese Seite)

- **Suche** (Suchfeld in der Kopfleiste): filtert die Mitarbeiterzeilen des Rasters nach
  **Name** (Nachname, Vorname, Zweitname, Ledigname oder Firma); eine rein numerische
  Eingabe sucht stattdessen nach der **ID-Nummer** (mehrere IDs mit Semikolon getrennt).
  Die Eingabe wird als Suchfilter an die Planungs-Abfrage übergeben und das Raster neu
  geladen; Leeren der Suche zeigt wieder alle Mitarbeitenden.
- **Gruppen-Auswahl** (Dropdown mit Gruppenbaum): begrenzt das Raster auf die gewählte
  Gruppe (Planungsblatt) inklusive aller Untergruppen. Beim Gruppenwechsel übernimmt die
  Seite zusätzlich das **Zahlungsintervall** der Gruppe (oder den Standard aus den
  Arbeitseinstellungen) — es bestimmt, ob wöchentlich, 14-täglich oder monatlich geblättert
  wird —, beendet ein aktives Szenario und lädt die Szenarien der neuen Gruppe. Die Wahl
  ist global und gilt gleichzeitig für alle Arbeitsbereich-Seiten.

### Toolbar (oben, in Render-Reihenfolge)

- **Perioden-Navigation**: Pfeil-Buttons ◀/▶ (`schedule-prev-btn`/`schedule-next-btn`) plus
  Perioden-Dropdown (`dropdownSetting`) mit Kalender-Picker; je nach Zahlungsintervall wird
  als Periode "KW n" (wöchentlich), "KW n–n+1" (14-täglich) oder der Monatsname angezeigt
  und geblättert.
- **E-Mail senden** (de: "E-Mail senden", en: "Send email", fr: "Envoyer un e-mail",
  it: "Invia e-mail", `schedule-send-email-btn`): verschickt die Einsatzpläne der sichtbaren
  Periode per E-Mail an die Mitarbeitenden des Planungsblatts; nur sichtbar, wenn E-Mail
  konfiguriert ist, und nur aktiv, wenn es seit dem letzten Versand ungesendete Änderungen
  gibt. Ergebnis-Toast: "{x} gesendet, {y} fehlgeschlagen, {z} ohne E-Mail".
- **Wizard-Button** (`schedule-wizard-btn`): standardmäßig Ein-Klick-**AutoWizard**
  (Tooltip de: "AutoWizard - Plan + Harmonizer + Holistic Harmonizer ausführen"); mit
  Ctrl+Shift+H (nur Admin) wechselt der Button in den Dropdown-Modus mit den Einzelschritten
  **Plan** (de/en/fr/it: "Plan"), **Fuzzy Harmonizer** und **Holistic Harmonizer**. Jeder
  Lauf schreibt sein Ergebnis als Szenario, das aktiviert wird und das der Planer übernehmen
  oder verwerfen muss.
- **Einsatzplan PDF** (de: "Einsatzplan PDF", en: "Schedule PDF", fr: "Planning PDF",
  it: "Pianificazione PDF", `schedule-pdf-export-btn`): exportiert die sichtbare Plantafel
  als PDF.
- Toggle **Gewünschte Absenzen ein-/ausblenden** (de: "Gewünschte Absenzen ein-/ausblenden",
  en: "Show/hide absence bars", fr: "Afficher/masquer les barres d'absence", it:
  "Mostra/nascondi barre assenza", `schedule-break-placeholder-toggle`, nur Tabellenmodus):
  blendet die Balken der vorgeplanten Absenz-Platzhalter aus dem Absenzen-Kalender ein/aus.
- Toggle **Befehle anzeigen** (de: "Befehle anzeigen", en: "Show Commands", fr: "Afficher
  les commandes", it: "Mostra comandi", `schedule-commands-toggle`, nur Tabellenmodus):
  blendet die Planungs-Befehle (Schedule-Commands wie FREI/FRÜH/SPÄT/NACHT-Vorgaben, die
  die Wizards respektieren müssen) im Raster ein/aus.
- Toggle **Verfügbarkeit ein-/ausblenden** (de: "Verfügbarkeit ein-/ausblenden", en:
  "Show/hide availability", fr: "Afficher/masquer la disponibilité", it: "Mostra/nascondi
  disponibilità", `schedule-availability-check-btn`): nur sichtbar, wenn
  Verfügbarkeitsdaten existieren.
- **Neuberechnen-Dropdown** (`schedule-recalculate-btn`): **Stunden zusammenzählen**
  (de: "Stunden zusammenzählen", en: "Sum hours", fr: "Totaliser les heures", it: "Somma
  ore") oder **Stunden berechnen und zusammenzählen** (de: "Stunden berechnen und
  zusammenzählen", en: "Calculate and sum hours", fr: "Calculer et totaliser les heures",
  it: "Calcola e somma ore") — die gründliche Variante fragt vor dem Start per
  Bestätigungs-Dialog nach.
- **Szenario-Auswahl** (`scenario-selector-btn`, Tooltip de: "Szenario auswählen", en:
  "Select scenario"): Button zeigt **Original** (de/en/fr: "Original", it: "Originale")
  oder den Namen des aktiven Szenarios. Menü im Szenario-Modus: **Zum Original** (de: "Zum
  Original", en: "To Original", fr: "Vers l'original", it: "All'originale"), **Szenario
  übernehmen** (en: "Accept scenario"), **Szenario verwerfen** (en: "Reject scenario"),
  **Szenario umbenennen** (en: "Rename scenario"); immer: Liste aller Szenarien der Gruppe,
  **+ Neues Szenario erstellen** (en: "Create new scenario") und **Alle Szenarien löschen**
  (en: "Delete all scenarios").
- Schalter **Tagesverlauf** (de: "Tagesverlauf", en: "Day Timeline", fr: "Déroulé de la
  journée", it: "Andamento giornaliero", `schedule-view-mode-toggle`): wechselt vom
  Tabellenraster zur Timeline-Ansicht. Dort erscheint der Bereichs-Dropdown
  (`timeline-range-dropdown`) mit **24 Std-Ansicht** (de: "24 Std-Ansicht", en: "24h View",
  fr: "Vue 24h", it: "Vista 24h") / **Tagesansicht** (de: "Tagesansicht", en: "Day View",
  fr: "Vue journalière", it: "Vista giornaliera"), und statt des Zoom-Sliders ein
  Zeilenhöhen-Slider (`schedule-timeline-row-height-slider`).
- **Zoom-Slider** (`schedule-zoom-slider`, Tabellenmodus): vergrößert/verkleinert das Raster.

### Zeilenkopf links (Mitarbeiter)

- Spaltentitel **Name** (de/en: "Name", fr: "Nom", it: "Nome"); die Zeilen werden auf einer
  Canvas gezeichnet. Pro Mitarbeiter gibt es drei Stunden-Slots mit Tooltips:
  **Garantierte Stunden** (en: "Guaranteed Hours"), **Geleistete Stunden** (en: "Worked
  Hours") und **Zuschlag** (en: "Surcharges"). Ein Warnsymbol mit Tooltip **Kein Vertrag**
  (de: "Kein Vertrag", en: "No Contract") markiert Mitarbeitende ohne passenden aktiven
  Vertrag im Zeitraum; ein Punkt mit Tooltip "Änderungen wurden noch nicht versendet"
  (en: "Changes have not been sent yet") markiert Zeilen mit ungesendeten Plan-Änderungen.
- Das Filter-Popup (`schedule-row-header-filter`) bietet Sortierung nach **Vorname**
  (en: "First Name"), **Nachname** (en: "Last Name"), **Firma** (en: "COMPANY") und
  **Vertraglich garantierte Stunden** (de: "Vertraglich garantierte Stunden", en:
  "Contractually guaranteed hours"), den Modus **Individuell Sortiert** (de: "Individuell
  Sortiert", en: "Individual Sort" — Zeilen per Drag-Handle umordnen) sowie die Checkboxen
  **Mitarbeiter** (en: "Employees") und **Extern** (en: "Ext").
- Rechtsklick auf eine Zeile: **Adresse anzeigen** (en: "Show address"), **Dienstplan
  ausdrucken** (en: "Print Staff Schedule"), **Dienstplan versenden** (en: "Send Staff
  Schedule") und **Dienst-Präferenzen...** (en: "Shift Preferences...").

### Planungsraster (Mitarbeiter × Tage)

- Zell-Typen: Dienst-Einträge, Korrekturen/Ablösungen/An- u. Abreise/Briefing,
  **Spesen/Vergütung** (steuerpflichtig = Spesen, nicht steuerpflichtig = Vergütung),
  Absenzen sowie Notizen und Planungs-Befehle.
- **Drag & Drop**: Schichten aus der unteren Schicht-Sektion in eine Zelle ziehen;
  bestehende Zelleinträge lassen sich verschieben oder per Zieh-Geste löschen.
  **Doppelklick** öffnet den passenden Editier-Dialog (Dienst, Korrektur/Spesen, Container).
- **Kontextmenü gefüllte Zelle**: Kopieren / Ausschneiden / Einfügen, Löschen,
  **Korrektur...** (en: "Correction..."), **Anreise/Abreise...** (en: "Travel...", fr:
  "Trajet...", it: "Viaggio..."), **Briefing/Debriefing...**, **Ablösung...** (en:
  "Replacement..."), **Spesen...** (en: "Expenses...", fr: "Frais...", it: "Spese..."),
  **Bearbeiten...**, **Bestätigen** (de: "Bestätigen", en: "Confirm") / **Bestätigung
  aufheben** (de: "Bestätigung aufheben", en: "Revoke confirmation"), **Im Schichtplan
  anzeigen** (en: "Show in shift schedule"). Bei Container-Diensten stattdessen
  **Öffnen...** (en: "Open...") und **Aufteilen** (en: "Split").
- **Kontextmenü leere Zelle**: **Einfügen** plus die Untermenüs **Dienste...** (de:
  "Dienste...", en: "Shifts...", fr: "Services...", it: "Turni...") — nur Schichten des
  Tages mit freier Kapazität — und **Beschäftigungen...** (de: "Beschäftigungen...", en:
  "Absences...", fr: "Absences...", it: "Assenze...") mit allen Absenz-Arten.
- **Balken "Gewünschte Absenzen"**: Rechtsklick auf einen Platzhalter-Balken bietet
  **Löschen** und **Absenz übernehmen** (de: "Absenz übernehmen", en: "Adopt absence") —
  das wandelt den vorgeplanten Platzhalter Tag für Tag in gebuchte Absenz-Einträge um.
- **Locks/Versiegelung**: Einträge mit LockLevel **Confirmed** (grüner Haken), **Approved**
  (gelbes Prüf-Symbol) oder **Closed** (rotes Schloss) sowie gruppenfremde (versiegelte)
  Einträge sind nicht editierbar. Bestätigen geht nur aus dem ungesperrten Zustand;
  Aufheben von Confirmed darf jeder Planer, von Approved nur Berechtigte/Admins, von
  Closed nur Admins. Auf versiegelten Tagesspalten und vor dem Eintrittsdatum eines
  Mitarbeiters (Beginn der Mitgliedschaft) bleibt das Kontextmenü leer — keine neuen
  Einträge möglich.
- Tooltips: Kopfzellen zeigen nicht gebuchte oder überbuchte Schichten, leere Zellen Feiertage.

### Unterer Bereich: Schicht-Sektion

- Tabs **Schichten** (de: "Schichten", en: "Shifts", fr: "Services", it: "Turni") und
  **Fehlerliste** (de: "Fehlerliste", en: "Error List", fr: "Liste des erreurs", it:
  "Lista errori") — mit Badges für Fehler (rot), Warnungen (gelb) und Infos (blau) aus
  der Kollisionsprüfung.
- Kopfzeile: **Filter**-Button (Schichten filtern), Zeilenzähler in Klammern und
  **Schichtplan PDF** (de: "Schichtplan PDF", en: "Shift schedule PDF", fr: "Plan de
  service PDF", it: "Piano turni PDF").
- Die Schichtzeilen laufen horizontal synchron zur Datumsachse des Rasters; von hier
  werden Schichten per Drag & Drop in das Raster gebucht.

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenbasis**: Die Zeilen sind Mitarbeitende und Externe (keine Kunden) mit einer im
  Zeitraum gültigen **Mitgliedschaft**; pro Zeile fließen aktive Verträge (für das
  "Kein Vertrag"-Symbol und die garantierten Stunden) und das Eintrittsdatum
  (Mitgliedschaftsbeginn) ein. Die Zellen liefert eine Stored Procedure, die Work,
  WorkChange, Spesen/Vergütung, Absenzen, Notizen und Befehle des Zeitraums zusammenführt —
  soft-gelöschte Datensätze sind überall ausgeschlossen. Dazu kommen separate
  Verfügbarkeitsdaten und die **Stundenbilanz** der Abrechnungsperiode; gebuchte
  Absenz-Einträge zählen mit ihrer hinterlegten Arbeitszeit zum Stunden-Soll.
- **Gruppen-Scope (Planungsblätter)**: Die gewählte Gruppe wirkt als Sichtbarkeits-Filter
  inklusive aller Untergruppen (rekursive Hierarchie); ohne Auswahl ist alles sichtbar.
  Sie bestimmt zudem das Zahlungsintervall (Wochen-/14-Tage-/Monats-Navigation) und welche
  Szenarien angeboten werden. Ein Gruppenwechsel beendet ein aktives Szenario.
- **Cross-Group-Einträge (modulare Planung)**: Buchungen, deren Schicht in einer fremden
  Gruppe lebt, werden NICHT ausgeblendet, sondern erscheinen als **versiegelt**
  (gruppengesperrt, schreibgeschützt) — der Planer sieht die Belegung jederzeit. Wichtig:
  Klacks verhindert Konflikte nicht still im Hintergrund, sondern macht sie sofort
  SICHTBAR — direkt in der Planung und in der **Fehlerliste** (Tab neben den Schichten,
  mit Badges für Fehler/Warnung/Info). Die Entscheidung bleibt beim Planer.
- **Szenarien**: Jedes Szenario ist eine isolierte Sandbox — seine Einträge
  tragen ein Analyse-Token und erscheinen exakt symmetrisch nur im Szenario-Modus, nie im
  Original (und umgekehrt). Wizard/Harmonizer-Läufe schreiben ihr Ergebnis als Szenario.
  **Übernehmen** schreibt die Szenario-Einträge in den echten Plan, **Verwerfen** löscht
  sie. Im Szenario-Modus wird die Tagessperren-Prüfung übersprungen (Sandbox), die
  Bearbeitung von **Schicht-Stammdaten** (Schicht bearbeiten/zuschneiden) ist dagegen
  blockiert, bis das Szenario übernommen oder verworfen ist.
- **Periodenabschluss (`/workplace/period-closing`)**: Versiegelte Tage lehnt das Backend
  bei Neuanlage und Änderung ab ("Day ... is sealed"); im Raster bleiben die Spalten
  gesperrt. Zusätzlich sperren die Eintrag-LockLevel (Confirmed/Approved/Closed) einzelne
  Zellen — Entsperren je nach Stufe nur durch Berechtigte/Admins.
- **Absenzen-Kalender (`/workplace/absence`)**: Dort erfasste Absenzen sind NUR vorgeplante
  Platzhalter (Erinnerungsstützen) — sie blockieren den Schichtplan nicht. Im Einsatzplan
  erscheinen sie als "Gewünschte Absenzen"-Balken; verbindlich wird eine Absenz erst, wenn
  sie hier als Absenz-Dienst gebucht wird (Untermenü "Beschäftigungen..." oder "Absenz
  übernehmen" auf dem Balken). Gebuchte Absenz-Dienste erscheinen im Absenzen-Kalender
  schreibgeschützt als "Gebucht".
- **E-Mail-Versand & Änderungs-Tracking**: Plan-Änderungen werden live (SignalR) pro
  Mitarbeiter als "ungesendet" markiert (Punkt in der Zeile); der Senden-Button ist nur
  aktiv, solange ungesendete Änderungen existieren.
- **Dashboard / Ressourcen-Monitor**: Der Dienste-Balken zählt geplante Dienste OHNE
  sporadische Dienste, Zeitfenster-Dienste (TimeRange), versiegelte Aufträge und
  Szenario-Einträge; Container zählen als 1. Der Absenzen-Balken umfasst vorgeplante
  Platzhalter UND gebuchte Absenz-Dienste.
- **Berechtigungen**: Der Wizard-Dropdown-Modus (Ctrl+Shift+H) ist Admins vorbehalten;
  Admins dürfen unabhängig vom LockLevel editieren und Closed-Sperren aufheben.

### Typische Aufgaben

- Plan ansehen/öffnen — Skills `open_schedule`, `read_schedule_state`
- Raster auf eine Gruppe eingrenzen oder wieder alle zeigen — Skill `select_group`
- Schicht buchen, Absenz, Spesen oder Korrektur erfassen — Skills `place_work`, `add_break`,
  `add_expense`, `add_workchange`, `delete_work`
- Einträge bestätigen / Bestätigung aufheben — Skills `confirm_work`, `unconfirm_work`
- Automatisch planen lassen — Skills `start_autowizard`, `start_wizard1`, `start_wizard2`,
  `start_wizard3`
- Szenarien verwalten — Skills `list_scenarios`, `accept_scenario`, `reject_scenario`,
  `evaluate_scenario`
- Konflikte prüfen, Ersatz finden, Plan versenden — Skills `detect_conflicts`,
  `find_replacement`, `cover_absence`, `email_schedule_to_client`
- Notizen/Befehle im Plan — Skills `add_schedule_note`, `add_schedule_command`
- Periode abschliessen/wieder öffnen — Skills `close_period`, `reopen_period`
- Zur Seite springen — Skill `navigate_to` (Ziel "schedule")

### Verwandte Seiten

- Schicht-Stammdaten: Schicht-Seite (`/workplace/shift`); Gruppen/Planungsblätter:
  Gruppen-Seite; Tagessperren: Periodenabschluss (`explain_page_period_closing`)
- `/workplace/absence` — Absenzen-Kalender (`explain_page_absence`); vorgeplante
  Platzhalter erscheinen hier als Balken, gebuchte Dienste dort als "Gebucht"
- `explain_planning_divide_et_impera`, `explain_planning_sheets_modular`,
  `explain_planning_assistant`
- `explain_shift_sporadic`, `explain_shift_time_range`, `explain_shift_container`,
  `explain_shift_lifecycle_order_to_shift`

### Trigger-Phrasen

- "Was sehe ich auf dem Einsatzplan?"
- "Erkläre diese Seite" (auf /workplace/schedule)
- "Was bedeuten die Buttons in der Dienstplan-Toolbar?"
- "Warum kann ich diesen Eintrag nicht bearbeiten?" (Lock/versiegelt/gruppenfremd)
- "Wie übernehme ich ein Szenario?"
- "What does the schedule grid show?"
- "How do I recalculate period hours?"
- "Que signifie cette vue du planning ?"
- "Cosa significa questa vista della pianificazione?"
