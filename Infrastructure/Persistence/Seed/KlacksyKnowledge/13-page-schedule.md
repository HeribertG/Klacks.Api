---
name: explain_page_schedule
description: |
  Explains the Schedule page (/workplace/schedule) — the central planning board with an
  employees-by-days grid, the toolbar (period navigation, AutoWizard/Plan/Fuzzy Harmonizer/
  Holistic Harmonizer, PDF export, absence bars, commands, availability, hour recalculation,
  scenario selector, day-timeline switch, zoom), the shift section with Shifts/Error List tabs,
  drag & drop booking, cell context menus, locks and sealed days. Use this when the user asks
  what they see on the Schedule page, what the cards/grid/columns/toolbar buttons mean, or how
  to work with it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - schedule
  - einsatzplan
  - dienstplan
  - schichtplan
  - plantafel
  - roster
  - planung
  - szenario
  - wizard
  - grid
synonyms:
  de: [einsatzplan, dienstplan, schichtplan, plantafel, "was sehe ich hier", "erkläre diese seite", "was bedeutet dieses raster", "was bedeutet diese ansicht"]
  en: [schedule, "schedule page", roster, "duty plan", "what do i see here", "explain this page", "what does this grid mean"]
  fr: [planning, "plan de service", horaire, "que vois-je ici", "explique cette page", "que signifie cette vue"]
  it: [pianificazione, "piano di servizio", turni, "cosa vedo qui", "spiega questa pagina", "cosa significa questa vista"]
---

# Einsatzplan — die Plantafel (/workplace/schedule)

## Zweck & Einstieg (1 Satz)

Der Einsatzplan ist die zentrale Plantafel von Klacks: ein Raster **Mitarbeiter × Tage**, in dem
Schichten per Drag & Drop platziert, geprüft, korrigiert und bestätigt werden. Erreichbar über die
Route `/workplace/schedule` oder das oberste Navi-Icon `open-schedules` (Tooltip **Alle Planungen**
(de: "Alle Planungen", en: "All Schedules", fr: "Tous les plannings", it: "Tutte le pianificazioni"),
Shortcut Alt+1). Welche Mitarbeiter-Zeilen sichtbar sind, bestimmt die globale **Gruppen-Auswahl**
im Kopfbereich der App — der Einsatzplan zeigt immer das aktuell gewählte Planungsblatt.

## Toolbar (oben, in Render-Reihenfolge)

- **Perioden-Navigation**: Pfeil-Buttons ◀/▶ (`schedule-prev-btn`/`schedule-next-btn`) plus
  Perioden-Dropdown mit Kalender; je nach Zahlungsintervall wöchentlich, 14-täglich oder monatlich.
- **E-Mail senden** (de: "E-Mail senden", en: "Send email", fr: "Envoyer un e-mail", it: "Invia e-mail")
  — verschickt die Einsatzpläne an die Mitarbeiter; nur sichtbar, wenn E-Mail konfiguriert ist.
- **Wizard-Button** (`schedule-wizard-btn`): entweder Ein-Klick-**AutoWizard** (de-Tooltip:
  "AutoWizard - Plan + Harmonizer + Holistic Harmonizer ausführen") oder Dropdown mit
  **Plan** (de/en/fr/it: "Plan"), **Fuzzy Harmonizer** und **Holistic Harmonizer** — die
  automatische Planung schreibt ihr Ergebnis als Szenario, das der Planer annehmen oder verwerfen muss.
- **Einsatzplan PDF** (de: "Einsatzplan PDF", en: "Schedule PDF", fr: "Planning PDF", it: "Pianificazione PDF").
- Toggle **Gewünschte Absenzen ein-/ausblenden** (de: "Gewünschte Absenzen ein-/ausblenden",
  en: "Show/hide absence bars", fr: "Afficher/masquer les barres d'absence", it: "Mostra/nascondi barre assenza").
- Toggle **Befehle anzeigen** (de: "Befehle anzeigen", en: "Show Commands", fr: "Afficher les commandes",
  it: "Mostra comandi") — blendet Schedule-Commands im Raster ein.
- Toggle **Verfügbarkeit ein-/ausblenden** (de: "Verfügbarkeit ein-/ausblenden", en: "Show/hide availability",
  fr: "Afficher/masquer la disponibilité", it: "Mostra/nascondi disponibilità") — nur sichtbar, wenn Verfügbarkeitsdaten existieren.
- **Neuberechnen-Dropdown** (`schedule-recalculate-btn`): **Stunden zusammenzählen** (en: "Sum hours")
  oder **Stunden berechnen und zusammenzählen** (en: "Calculate and sum hours").
- **Szenario-Auswahl** (`scenario-selector-btn`, Tooltip de: "Szenario auswählen"): Button zeigt
  **Original** (de/en/fr: "Original", it: "Originale") oder den Namen des aktiven Szenarios. Menü:
  **Zum Original** (en: "To Original"), **Szenario übernehmen** (en: "Accept scenario"),
  **Szenario verwerfen** (en: "Reject scenario"), **Szenario umbenennen**, **Neues Szenario erstellen**,
  **Alle Szenarien löschen**. Im Szenario-Modus ist Schicht-Bearbeitung gesperrt, bis das Szenario
  übernommen oder verworfen wird.
- Schalter **Tagesverlauf** (de: "Tagesverlauf", en: "Day Timeline", fr: "Déroulé de la journée",
  it: "Andamento giornaliero") wechselt vom Tabellenraster zur Timeline-Ansicht; dort gibt es den
  Bereichs-Dropdown **24 Std-Ansicht** (en: "24h View") / **Tagesansicht** (en: "Day View"), und der
  Zoom-Slider wird zum Zeilenhöhen-Slider.
- **Zoom-Slider** (Tabellenmodus, `schedule-zoom-slider`).

## Oberer Bereich: Planungsraster (Mitarbeiter × Tage)

- Links der Zeilenkopf mit Spalte **Name** (de/en: "Name", fr: "Nom", it: "Nome"): die Mitarbeiter des
  gewählten Planungsblatts. Ein Filterfenster bietet Sortierung nach Vorname, Name, Firma und
  **Vertraglich garantierte Stunden** (en: "Contractually guaranteed hours"), den Modus
  **Individuell Sortiert** (en: "Individual Sort", Zeilen per Drag-Handle umordnen) sowie die
  Checkboxen **Mitarbeiter** (en: "Employees") und **Extern** (en: "Ext").
- Zell-Typen: Arbeitseinträge (Work), Korrekturen/Ablösungen (WorkChange), Spesen/Vergütung
  (Expenses) und Absenzen (Break).
- **Drag & Drop**: Schichten aus der unteren Schicht-Sektion in eine Zelle ziehen (Overlay zeigt
  gültig/ungültig); bestehende Zelleinträge lassen sich verschieben oder per Zieh-Geste löschen.
- **Doppelklick** öffnet den passenden Editier-Dialog (Work, WorkChange/Spesen, Container).
- **Kontextmenü** (gefüllte Zelle): Kopieren / Ausschneiden / Einfügen, Löschen, **Korrektur...**,
  **Anreise/Abreise...** (en: "Travel..."), **Briefing/Debriefing...**, **Ablösung...** (en: "Replacement..."),
  **Spesen...** (en: "Expenses..."), **Bearbeiten...**, **Bestätigen** (en: "Confirm") /
  **Bestätigung aufheben** (en: "Revoke confirmation"), **Im Schichtplan anzeigen**. Bei
  Container-Diensten zusätzlich **Öffnen...** und **Aufteilen** (en: "Split"). Leere Zellen bieten
  **Einfügen** plus die Untermenüs **Dienste...** (en: "Shifts...") und **Beschäftigungen...** (en: "Absences...").
- **Locks/Versiegelung**: Einträge mit LockLevel Confirmed/Approved/Closed sowie gruppenfremde
  (gruppengesperrte) Einträge sind nicht editierbar; auf versiegelten Tagesspalten und vor dem
  Eintrittsdatum eines Mitarbeiters lassen sich keine neuen Einträge anlegen.
- Tooltips: Kopfzellen zeigen verfügbare/überbuchte Schichten, leere Zellen Feiertage.

## Unterer Bereich: Schicht-Sektion

- Tabs **Schichten** (de: "Schichten", en: "Shifts", fr: "Services", it: "Turni") und **Fehlerliste**
  (de: "Fehlerliste", en: "Error List", fr: "Liste des erreurs", it: "Lista errori") — mit Badges für
  Fehler (rot), Warnungen (gelb) und Infos (blau) aus der Kollisionsprüfung.
- Kopfzeile: **Filter**-Button (Schichten filtern), Zeilenzähler und **Schichtplan PDF**
  (en: "Shift schedule PDF", fr: "Plan de service PDF", it: "Piano turni PDF").
- Die Schichtzeilen laufen horizontal synchron zur Datumsachse des Rasters; von hier werden
  Schichten per Drag & Drop in das Raster gebucht.

## Typische Aufgaben (+ Klacksy-Skills)

- Plan ansehen/öffnen → `open_schedule`, `read_schedule_state`
- Schicht buchen, Absenz oder Spesen/Korrektur erfassen → `place_work`, `add_break`, `add_expense`, `add_workchange`, `delete_work`
- Einträge bestätigen / Bestätigung aufheben → `confirm_work`, `unconfirm_work`
- Automatisch planen lassen → `start_autowizard`, `start_wizard1`, `start_wizard2`, `start_wizard3`
- Szenarien verwalten → `list_scenarios`, `accept_scenario`, `reject_scenario`, `evaluate_scenario`
- Konflikte prüfen, Ersatz finden, Plan versenden → `detect_conflicts`, `find_replacement`, `cover_absence`, `email_schedule_to_client`
- Notizen/Befehle im Plan → `add_schedule_note`, `add_schedule_command`

## Verwandte Seiten & Happen

- Schicht-Stammdaten: Shift-Seite; Gruppen/Planungsblätter: Gruppen-Seite
- `explain_planning_divide_et_impera`, `explain_planning_sheets_modular`, `explain_planning_assistant`
- `explain_shift_sporadic`, `explain_shift_time_range`, `explain_shift_container`, `explain_shift_lifecycle_order_to_shift`

## Trigger-Phrasen

- "Was sehe ich auf dem Einsatzplan?"
- "Erkläre diese Seite" (auf /workplace/schedule)
- "Was bedeuten die Buttons in der Dienstplan-Toolbar?"
- "What does the schedule grid show?"
- "Que signifie cette vue du planning ?"
- "Cosa significa questa vista della pianificazione?"
