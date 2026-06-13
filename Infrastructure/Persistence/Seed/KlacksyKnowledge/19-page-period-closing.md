---
name: explain_page_period_closing
description: |
  Explains the Period Closing page (Periodenabschluss) at /workplace/period-closing — the
  admin-only page for sealing billing periods day by day. Covers the Periods card (period
  dropdown grouped by payment interval, sealed/partial/empty day badges, the day table with
  seal checkboxes, the mandatory-reason unseal dialog, the collapsible issues card with
  error/warning/info badges and the unstaffed-shift counter, bulk seal/unseal), the Exports
  card (date filter, sealed-order search, CSV/JSON/XML/DATEV/BMD formats, proof-of-service
  download with partial-close warning), and the Audit Log card (seal/unseal actions and
  export history with detail modals). Use this when the user asks what they see on the
  Period Closing page, what the cards/columns/badges mean, how to seal or reopen a period,
  why a sealed day rejects changes, or who sealed/exported what. Supports a level parameter:
  short (purpose only), elements (every element explained), effects (day-lock impact on
  schedule/absences, audit and export logs, permissions and interplay with other pages).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - period closing
  - periodenabschluss
  - periode
  - versiegeln
  - entsiegeln
  - seal
  - unseal
  - seal period
  - reopen period
  - lock period
  - periode versiegeln
  - periode öffnen
  - tage versiegeln
  - schichten blockieren
  - seal shifts
  - abrechnungsperiode
  - billing period
  - leistungsnachweis
  - proof of service
  - export log
  - exportprotokoll
  - protokoll
  - audit
  - audit log
  - abschluss
  - begründung erforderlich
  - reason required
  - unverplante shifts
  - unstaffed shifts
  - alle versiegeln
  - alle entsiegeln
  - betroffene einträge
  - affected entries
  - datev
  - bmd
synonyms:
  de: [periodenabschluss, was sehe ich hier, erkläre diese seite, was bedeutet diese karte, periode versiegeln, periode öffnen, periode entsiegeln, tag versiegeln, tag wieder öffnen, alle versiegeln, alle entsiegeln, warum brauche ich eine begründung, warum kann ich diesen tag nicht ändern, abrechnungsperiode abschliessen, leistungsnachweis exportieren, probleme dieser periode, unverplante shifts, exportprotokoll, wer hat versiegelt, abschluss-seite]
  en: [period closing, what do I see here, explain this page, what does this card mean, seal period, reopen period, unseal day, seal all, unseal all, why do I need a reason, why can't I change this day, close billing period, export proof of service, issues in this period, unstaffed shifts, export log, audit log, who sealed the period]
  fr: [clôture de période, qu'est-ce que je vois ici, explique cette page, que signifie cette carte, sceller la période, rouvrir la période, sceller un jour, pourquoi un motif est requis, pourquoi je ne peux pas modifier ce jour, problèmes de cette période, shifts non planifiés, journal d'audit, journal des exports, qui a scellé la période, exporter le justificatif]
  it: [chiusura periodo, cosa vedo qui, spiega questa pagina, cosa significa questa scheda, sigilla periodo, riapri periodo, sigillare un giorno, perché serve un motivo, perché non posso modificare questo giorno, problemi di questo periodo, turni non pianificati, registro di controllo, registro esportazioni, chi ha sigillato il periodo, esportare il giustificativo]
---

# Periodenabschluss — die Seite /workplace/period-closing

<!-- level:short -->

## Stufe 1 — Wofür ist diese Seite?

Der **Periodenabschluss** (de: "Periodenabschluss", en: "Period Closing", fr: "Clôture de
période", it: "Chiusura periodo") ist die Admin-Seite zum Versiegeln von Abrechnungsperioden:
Tag für Tag oder als ganze Periode werden Arbeits- und Absenz-Einträge auf die höchste
Sperrstufe gesetzt, sodass das Backend danach jede Änderung an diesen Tagen ablehnt.
Zusätzlich werden hier Leistungsnachweise versiegelter Bestellungen exportiert
(CSV/JSON/XML/DATEV/BMD) und jede Versiegelung, Öffnung und jeder Export im Protokoll
nachvollzogen. Erreichbar über das Navi-Icon `open-period-closing` (nur für Admins sichtbar)
oder direkt unter `/workplace/period-closing`; die Seite zeigt drei Karten untereinander:
**Perioden**, **Exporte**, **Protokoll**.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

### Globale Kopfleiste der App (oberster Rand)

- **Suche**: Das globale Suchfeld der Kopfleiste ist auf dieser Seite **ausgeblendet** — die
  Seite blendet es beim Öffnen aktiv aus, und es gibt keine eigene Such-Strategie für den
  Periodenabschluss. Gesucht wird stattdessen lokal in der Exporte-Karte (Bestellungs-Suchfeld).
- **Gruppen-Auswahl**: Die globale Gruppen-Auswahl wirkt auf dieser Seite **nicht** (sie
  steuert Stammdaten, Absenzen, Schichtplan, Shifts und Verfügbarkeit). Der Gruppenbezug
  kommt hier aus der gewählten Periode selbst: Perioden-Einträge im Dropdown können einen
  Gruppennamen tragen, und Versiegeln/Entsiegeln/Problemliste laufen dann auf diese Gruppe
  begrenzt.

### Karte **Perioden** (de: "Perioden", en: "Periods", fr: "Périodes", it: "Periodi")

- Dropdown **Periode** (de: "Periode", en: "Period", fr: "Période", it: "Periodo"), DOM-ID
  `periodsSelect`: listet alle verbuchten Perioden — Zeitfenster der Mitarbeiter-Periodenstunden,
  die tatsächlich nicht gelöschte Arbeits- oder Absenz-Einträge enthalten — gruppiert nach
  Zahlungsintervall: **Wöchentlich** (de: "Wöchentlich", en: "Weekly", fr: "Hebdomadaire",
  it: "Settimanale"), **Zweiwöchentlich** (en: "Biweekly"), **Monatlich** (en: "Monthly"),
  **Individuell** (en: "Individual"). Das Label zeigt den Zeitraum plus ggf. den Gruppennamen;
  monatliche Perioden werden auf den vollen Kalendermonat normalisiert. Daneben ein
  Aktualisieren-Button. Ohne Daten: **Keine verbuchten Perioden vorhanden.** (en: "No booked
  periods available.").
- Badge-Zeile über der Tabelle: Anzahl **versiegelter Tage** (z. B. "12/31 ✓"), Anzahl
  **teilweise versiegelter Tage** (Tage mit Einträgen, aber ohne Tagessiegel) und Anzahl
  **leerer Tage** (ohne Arbeits-/Absenz-Einträge), jeweils bezogen auf alle Tage der Periode.
- Aufklappbare Tabelle **Tage der Periode** (de: "Tage der Periode", en: "Days of the period",
  fr: "Jours de la période", it: "Giorni del periodo") mit drei Spalten:
  - **VERSIEGELT / GESAMT** (de: "VERSIEGELT / GESAMT", en: "SEALED / TOTAL", fr:
    "SCELLÉES / TOTAL", it: "SIGILLATE / TOTALE"): eine Checkbox pro Tag. **Anhaken
    versiegelt den Tag sofort** (ohne Begründung); **Abhaken öffnet den Inline-Dialog
    Begründung** (de: "Begründung", en: "Reason", fr: "Motif", it: "Motivo"), Textfeld
    `unsealReason` mit Placeholder "Warum wird diese Periode wieder geöffnet?" (en: "Why is
    this period being reopened?") und den Buttons **Periode wieder öffnen** (en: "Reopen
    period") und **Abbrechen** (en: "Cancel"). Ohne Begründung erscheint der Hinweis-Toast
    "Eine Begründung ist beim Öffnen einer Periode erforderlich." (en: "A reason is required
    when reopening a period."). Während des Versiegelns zeigt die Zelle einen Spinner.
  - **DATUM** (en: "DATE"): der Kalendertag.
  - **STATUS**: z. B. "3/5 Arbeit, 1/2 Absenz" (de: "Arbeit"/"Absenz", en: "Work"/"Absence")
    — versiegelte/gesamte Work-Einträge und versiegelte/gesamte Absenz-Einträge des Tages.
- Karte **Probleme dieser Periode** (de: "Probleme dieser Periode", en: "Issues in this
  period", fr: "Problèmes de cette période", it: "Problemi di questo periodo"), initial
  zugeklappt:
  - Badges je Schweregrad mit Zähler: **Fehler / Warnung / Hinweis** (de: "Fehler"/"Warnung"/
    "Hinweis", en: "Error"/"Warning"/"Note", fr: "Erreur"/"Avertissement"/"Note", it:
    "Errore"/"Avviso"/"Nota") plus ein Badge **N unverplante Shifts** (de: "unverplante
    Shifts", en: "unstaffed shifts", fr: "shifts non planifiés", it: "turni non pianificati")
    — berechnet als Bedarf (Mitarbeiterbedarf × Anzahl) minus tatsächlich gebuchte Personen;
    ein "+" am Zähler bedeutet, dass nicht alle Shifts geladen wurden (Lade-Limit 10000) und
    die Zahl eine Untergrenze ist. Ohne Befunde: "Keine Probleme gefunden".
  - Darunter die Problemliste, nach Datum gruppiert, je Eintrag Mitarbeitername + Meldung.
  - PDF-Button (Tooltip **Probleme als PDF exportieren**, en: "Export issues as PDF"),
    sichtbar nur wenn Probleme vorhanden sind; exportiert die Problemliste als PDF.
- Bulk-Buttons unten: **Alle versiegeln** (de: "Alle versiegeln", en: "Seal all") —
  deaktiviert, wenn schon alle Tage versiegelt sind — und **Alle entsiegeln** (de: "Alle
  entsiegeln", en: "Unseal all") — deaktiviert ohne versiegelte Tage, öffnet denselben
  Begründungs-Dialog. Erfolg wird als Toast gemeldet: "{count} Einträge versiegelt." bzw.
  "{count} Einträge wieder geöffnet."

### Karte **Exporte** (de: "Exporte", en: "Exports", fr: "Exports", it: "Esportazioni")

- Datumsfilter **Von / Bis** (de: "Von"/"Bis", en: "From"/"Until", fr: "Du"/"Au", it:
  "Dal"/"Al"), DOM-IDs `exportsFilterFrom`/`exportsFilterUntil`; Standard ist der 1. des
  Vormonats bis zum Ende des aktuellen Monats, daneben ein Aktualisieren-Button.
- Suchfeld **Bestellung** (de: "Bestellung", en: "Order", fr: "Commande", it: "Ordine"),
  DOM-ID `exportsSearch`, mit Placeholder "Kürzel, Name, Kunde, Kundennummer…" (en:
  "Abbreviation, name, customer, customer number…"): sucht versiegelte Bestellungen
  im Zeitfenster (300 ms Tipp-Verzögerung); Treffer erscheinen als Dropdown im
  Format "Kürzel – Name – Kunde – Zeitraum – geschlossen/gesamt"; ohne Treffer: "Keine
  Bestellungen im gewählten Zeitfenster gefunden."
- Dropdown **Format** (en: "Format"), DOM-ID `exportFormat`: CSV, JSON, XML, DATEV, BMD NTCS.
- Button **Exportieren** (de: "Exportieren", en: "Export", fr: "Exporter", it: "Esporta"):
  deaktiviert ohne gewählte Bestellung; lädt den Leistungsnachweis direkt als Datei im
  Browser herunter (Erfolgs-Toast "Export erzeugt: {file}"). Sind nicht alle Schichten
  geschlossen, erscheint vorher die Warnung "X von Y Schichten sind geschlossen. Nicht
  geschlossene Schichten werden NICHT exportiert." (en: "... Non-closed shifts will NOT be
  exported."). Die Export-Sprache ist die aktuelle UI-Sprache; der Server legt nach
  erfolgreichem Download automatisch einen Eintrag im Export-Protokoll an.

### Karte **Protokoll** (de: "Protokoll", en: "Audit Log", fr: "Journal d'audit", it: "Registro di controllo")

- Datumsfilter **Startdatum / Enddatum** (de: "Startdatum"/"Enddatum", en: "Start date"/
  "End date"), DOM-IDs `auditStartDate`/`auditEndDate`; Standard ist der aktuelle Monat,
  daneben ein Aktualisieren-Button.
- Aufklappbare Sektion **Aktionen** (de: "Aktionen", en: "Actions", fr: "Actions", it:
  "Azioni") mit Tabelle `audit-log-table`, Spalten **DATUM | AKTION | BENUTZER | BETROFFENE
  EINTRÄGE** (en: DATE | ACTION | USER | AFFECTED ENTRIES); die Aktion ist ein Badge
  **Versiegelt** (de: "Versiegelt", en: "Sealed", fr: "Scellée", it: "Sigillato") oder
  **Wieder geöffnet** (de: "Wieder geöffnet", en: "Reopened", fr: "Rouverte", it: "Riaperto").
  Klick (oder Enter) auf eine Zeile öffnet das Modal **Protokoll-Eintrag** (de:
  "Protokoll-Eintrag", en: "Audit Entry") mit Aktion, Ausgeführt am/von, Zeitraum, Gruppe
  (oder **Alle Gruppen**, en: "All groups"), Betroffene Einträge und Begründung.
- Aufklappbare Sektion **Exporte** (en: "Exports") mit Tabelle `export-log-table`, Spalten
  **ERZEUGT AM | FORMAT | DATEI | BENUTZER | EINTRÄGE** (en: CREATED AT | FORMAT | FILE |
  USER | RECORDS). Klick auf eine Zeile öffnet das Modal **Export-Eintrag** (de:
  "Export-Eintrag", en: "Export Entry") mit Format, Erzeugt am/von, Zeitraum, Gruppe,
  Dateiname, Grösse (formatiert in B/KB/MB/GB), Anzahl Einträge, Sprache und Währung.
- Leere Zeiträume zeigen "Keine Einträge im ausgewählten Zeitraum." bzw. "Keine Exporte im
  ausgewählten Zeitraum."

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Was Versiegeln tut**: Das Versiegeln setzt alle Dienst- und Absenz-Einträge
  des Zeitraums auf die höchste der vier Sperrstufen (None → Confirmed → Approved →
  **Closed**) und legt zusätzlich pro Kalendertag ein **Tagessiegel** an — mit Gruppenbezug,
  wenn die gewählte Periode eine Gruppe trägt, sonst global für alle. Der gemeldete Zähler
  "betroffene Einträge" ist die Summe aus versiegelten Dienst- und Absenz-Einträgen plus neu
  angelegten Tagessiegeln. Der Protokoll-Eintrag wird in derselben Transaktion geschrieben.
- **Wirkung des Tagessiegels**: Für versiegelte Tage lehnt das Backend jede Neuanlage,
  Änderung und Löschung von Dienst-Einträgen, Absenzen, Spesen und Korrekturen ab —
  mit der Fehlermeldung **Tag ist gesperrt und kann nicht geändert werden** (de: "Tag ist
  gesperrt und kann nicht geändert werden", en: "Day is sealed and cannot be modified", fr:
  "Le jour est scellé et ne peut être modifié", it: "Il giorno è sigillato e non può essere
  modificato"). Ein Tagessiegel **ohne** Gruppe sperrt den Tag für alle; ein Tagessiegel
  **mit** Gruppe sperrt nur Mitarbeitende, deren Work an diesem Tag über den Shift zu dieser
  Gruppe gehört.
- **Szenario-Isolation**: Einträge aus Analyse-Szenarien (des
  Planungs-Assistenten) umgehen die Tagessperre — die Prüfung wird für Szenario-Einträge
  übersprungen; verbindlich wird die Sperre erst beim Übernehmen ins echte Schedule. Auch die
  Problemliste berücksichtigt nur reale Planungs-Notizen (keine Szenario-Notizen).
- **Entsiegeln**: verlangt zwingend eine Begründung (das Backend lehnt ohne Begründung ab),
  setzt die Sperrstufe der Works/Breaks zurück auf None und löscht die Tagessiegel (Soft
  Delete); auch dies schreibt einen Protokoll-Eintrag (Wieder geöffnet) mit der Begründung.
  Bereits erzeugte Exporte bleiben im Export-Protokoll erhalten — ein erneuter Export nach
  Korrekturen kann anderen Inhalt haben; das System warnt davor nicht automatisch.
- **Berechtigungen**: Versiegeln auf Stufe Closed und Entsiegeln von Closed kann **nur die
  Admin-Rolle**; auch das Navi-Icon `open-period-closing` ist nur für Admins sichtbar. Die
  Zwischenstufen (Bestätigt/Freigegeben) werden nicht hier, sondern im Schichtplan bzw. über
  die Skills `approve_day`/`revoke_day_approval` verwaltet — Freigeben braucht das
  "berechtigt"-Recht oder Admin.
- **Problemliste**: aggregiert manuelle Planungs-Notizen aus dem Schichtplan (max. 500) plus
  die Live-Befunde des Schedule-Validators (Kollisionen, Ruhezeiten, Überstunden,
  Konsekutivtage) über den Zeitraum; gelöschte Mitarbeitende und gelöschte Notizen sind
  ausgeschlossen, der Gruppenfilter der Periode greift auch hier. Die unverplanten Shifts
  stammen aus dem Schichtplan-Datenbestand (Bedarf minus besetzt).
- **Export-Karte und versiegelte Bestellung**: Der Export hängt an der **versiegelten Bestellung**
  aus dem Schicht-Lebenszyklus — spätere Umbenennungen oder Schnitte der
  operativen Schicht verändern das exportierte Dokument nicht. Jeder erfolgreiche Download
  erzeugt automatisch einen Export-Protokoll-Eintrag (Format, Zeitraum, Gruppe, Dateiname,
  Grösse, Anzahl Einträge, Sprache, Währung, Benutzer).
- **Protokoll (Audit-Log)**: Jede Versiegelung und Öffnung schreibt einen Eintrag mit Aktion,
  Zeitraum, Gruppe, Begründung, Anzahl betroffener Einträge, Zeitstempel und Benutzer — die
  lückenlose Antwort auf "wer hat wann was versiegelt oder geöffnet".
- **Assistent**: Klacksy überwacht die Perioden-Enden der Gruppen — endet das laufende
  Zahlungsintervall (Woche, 14 Tage oder Monat) in höchstens 3 Tagen und ist der End-Tag noch
  nicht versiegelt, kann der Assistent proaktiv an den Abschluss erinnern (Intervall
  "Individuell" ohne festen Zyklus ist ausgenommen).
- **Schichtplan (`/workplace/schedule`)**: Dort entstehen die Work-Einträge, die hier
  versiegelt werden; Änderungen an versiegelten Tagen schlagen dort mit der
  Tagessperre-Fehlermeldung fehl.
- **Absenzen Kalender (`/workplace/absence`)**: Neuanlage und Änderung von Absenzen in
  versiegelten Zeiträumen werden vom Backend ebenso abgelehnt — erst entsiegeln, dann
  korrigieren.

### Typische Aufgaben

- Monat zum Abrechnungsende versiegeln → Periode wählen, Probleme-Karte prüfen, dann
  **Alle versiegeln** — oder per Klacksy-Skill `close_period`.
- Einzelnen Tag nachträglich korrigieren → Checkbox abhaken + Begründung — oder Skill
  `reopen_period`; Tage freigeben/Freigabe zurücknehmen vor dem Abschluss: `approve_day` /
  `revoke_day_approval`.
- Vor dem Abschluss offene Probleme prüfen (Fehler/Warnungen/unverplante Shifts) und als PDF
  sichern; eine Kurzübersicht liefert der Skill `generate_period_summary`.
- Leistungsnachweis für einen Kunden exportieren → Bestellung suchen, Format wählen,
  **Exportieren** — Skills: `list_sealed_orders`, `open_order_export`.
- Nachvollziehen, wer wann versiegelt/geöffnet oder exportiert hat → Protokoll-Karte; per
  Chat: `list_recent_exports`.
- Zur Seite springen — Skill `navigate_to` (Ziel "period-closing").

### Verwandte Seiten

- **Schichtplan** (`/workplace/schedule`): Quelle der Work-Einträge und Ort der
  Zwischenstufen Bestätigt/Freigegeben; versiegelte Tage sind dort gesperrt.
- **Absenzen Kalender** (`/workplace/absence`): Absenzen in versiegelten Zeiträumen sind
  ebenfalls gesperrt.
- `explain_shift_lifecycle_order_to_shift` — was eine versiegelte Bestellung ist und warum der Export an
  ihr hängt.

### Trigger-Phrasen

- "Was sehe ich auf der Periodenabschluss-Seite?"
- "Wie versiegle ich den Monat / die Periode?"
- "Warum kann ich diesen Tag im Schichtplan nicht mehr ändern?" (Tag versiegelt)
- "Warum brauche ich eine Begründung zum Entsiegeln?"
- "Wer hat diese Periode versiegelt / wieder geöffnet?"
- "How do I export the proof of service as DATEV/BMD?"
