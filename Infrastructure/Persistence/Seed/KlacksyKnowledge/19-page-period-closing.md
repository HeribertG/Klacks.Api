---
name: explain_page_period_closing
description: |
  Explains the Period Closing page (Periodenabschluss) at /workplace/period-closing — the
  admin-only page for sealing billing periods day by day. Covers the Periods card (period
  dropdown grouped by payment interval, sealed/partial/empty day badges, the day table with
  seal checkboxes, the mandatory-reason unseal dialog, the collapsible issues card with
  error/warning/info badges and the unstaffed-shift counter, bulk seal/unseal), the Exports
  card with its three sub-tabs (Single Order proof of service, Employee Hours, Orders in
  Range as ZIP) and the export formats offered, and the Audit Log card (seal/unseal/approve/
  confirm actions and export history with detail modals). Also explains automatic closing:
  Klacksy closes a period on its own only at autonomy level Fully autonomous and only after
  the closing time (days after the period end) was agreed in the conversation. Use this when
  the user asks what they see on the Period Closing page, what the cards/columns/badges mean,
  how to seal or reopen a period, why a sealed day rejects changes, who sealed/exported what,
  or when a period is closed automatically. Supports a level parameter:
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
  - mitarbeiterstunden
  - zeitraum export
  - orders in range
  - automatisch abschliessen
  - auto close
  - abschluss zeitpunkt
  - wann wird die periode abgeschlossen
synonyms:
  de: [periodenabschluss, was sehe ich hier, erkläre diese seite, was bedeutet diese karte, periode versiegeln, periode öffnen, periode entsiegeln, tag versiegeln, tag wieder öffnen, alle versiegeln, alle entsiegeln, warum brauche ich eine begründung, warum kann ich diesen tag nicht ändern, abrechnungsperiode abschliessen, leistungsnachweis exportieren, probleme dieser periode, unverplante shifts, exportprotokoll, wer hat versiegelt, abschluss-seite, mitarbeiterstunden exportieren, zeitraum als zip exportieren, periode automatisch abschliessen, wann wird die periode abgeschlossen]
  en: [period closing, what do I see here, explain this page, what does this card mean, seal period, reopen period, unseal day, seal all, unseal all, why do I need a reason, why can't I change this day, close billing period, export proof of service, issues in this period, unstaffed shifts, export log, audit log, who sealed the period, export employee hours, export orders as zip, close periods automatically, when is the period closed]
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
Zusätzlich werden hier drei Arten von Exporten erzeugt — Leistungsnachweis einer
versiegelten Bestellung, Mitarbeiterstunden eines Zeitraums und alle versiegelten Bestellungen
eines Zeitraums als ZIP — und jede Versiegelung, Öffnung und jeder Export im Protokoll
nachvollzogen. Erreichbar über das Navi-Icon `open-period-closing` (nur für Admins sichtbar)
oder direkt unter `/workplace/period-closing`; die Seite zeigt drei Karten untereinander:
**Perioden**, **Exporte**, **Protokoll**.

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

Die Seite hat drei Karten untereinander (Perioden, Exporte, Protokoll) mit Unterelementen. Alle
tragen Anker-IDs (unten in Backticks), über die Klacksy mit `get_page_controls` und in der
geführten Tour direkt zu einem Element scrollen kann. Die Exporte-Karte hat drei Unter-Tabs;
ein Anker in einem nicht angezeigten Unter-Tab schaltet den Tab automatisch um. Die Karte
**Probleme dieser Periode** klappt sich beim Anspringen selbst auf.

ANWEISUNG AN DEN ASSISTENTEN: Wenn du dem Benutzer mehrere Elemente dieser Seite nacheinander
erklärst, MUSST du vor jeder Erklärung den Scroll-Marker `[SCROLL:anker-id]` mit der Anker-ID des
Elements einfügen (z. B. `[SCROLL:period-closing-period-select]` vor „Periode",
`[SCROLL:period-closing-issues]` vor „Probleme dieser Periode",
`[SCROLL:period-closing-export-tab-range]` vor „Bestellungen im Zeitraum",
`[SCROLL:period-closing-audit-actions]` vor „Aktionen"). Der Client blendet die Marker aus und
scrollt die Seite mit — erwähne die Marker und Anker-IDs niemals im Text. Verwende nur die Anker
dieser Seite (Präfix `period-closing`); die Kartenanker sind `period-closing-periods`,
`period-closing-exports`, `period-closing-audit`.

### Globale Kopfleiste der App (oberster Rand)

- **Suche**: Das globale Suchfeld der Kopfleiste ist auf dieser Seite **ausgeblendet** — die
  Seite blendet es beim Öffnen aktiv aus, und es gibt keine eigene Such-Strategie für den
  Periodenabschluss. Gesucht wird stattdessen lokal in der Exporte-Karte (Bestellungs-Suchfeld).
- **Gruppen-Auswahl**: Die globale Gruppen-Auswahl wirkt auf dieser Seite **nicht** (sie
  steuert Stammdaten, Absenzen, Schichtplan, Shifts und Verfügbarkeit). Der Gruppenbezug
  kommt hier aus der gewählten Periode selbst: Perioden-Einträge im Dropdown können einen
  Gruppennamen tragen, und Versiegeln/Entsiegeln/Problemliste laufen dann auf diese Gruppe
  begrenzt.

### Karte **Perioden** (de: "Perioden", en: "Periods", fr: "Périodes", it: "Periodi") — Anker `period-closing-periods`

- Dropdown **Periode** (de: "Periode", en: "Period", fr: "Période", it: "Periodo"), Anker
  `period-closing-period-select`, DOM-ID `periodsSelect`: listet alle verbuchten Perioden — Zeitfenster der Mitarbeiter-Periodenstunden,
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
  fr: "Jours de la période", it: "Giorni del periodo"), Anker `period-closing-day-table`
  (erscheint erst, wenn die Tage der gewählten Periode geladen sind), mit drei Spalten:
  - **VERSIEGELT / GESAMT** (de: "VERSIEGELT / GESAMT", en: "SEALED / TOTAL", fr:
    "SCELLÉES / TOTAL", it: "SIGILLATE / TOTALE"): eine Checkbox pro Tag. **Anhaken
    versiegelt den Tag sofort** (ohne Begründung); **Abhaken öffnet den Inline-Dialog
    Begründung** (de: "Begründung", en: "Reason", fr: "Motif", it: "Motivo"; Anker
    `period-closing-unseal-reason` — der Dialog existiert nur, solange er offen ist, also erst
    nach einem Klick auf Abhaken bzw. **Alle entsiegeln**), Textfeld
    `unsealReason` mit Placeholder "Warum wird diese Periode wieder geöffnet?" (en: "Why is
    this period being reopened?") und den Buttons **Periode wieder öffnen** (en: "Reopen
    period") und **Abbrechen** (en: "Cancel"). Ohne Begründung erscheint der Hinweis-Toast
    "Eine Begründung ist beim Öffnen einer Periode erforderlich." (en: "A reason is required
    when reopening a period."). Während des Versiegelns zeigt die Zelle einen Spinner.
  - **DATUM** (en: "DATE"): der Kalendertag.
  - **STATUS**: z. B. "3/5 Arbeit, 1/2 Absenz" (de: "Arbeit"/"Absenz", en: "Work"/"Absence")
    — versiegelte/gesamte Work-Einträge und versiegelte/gesamte Absenz-Einträge des Tages.
- Karte **Probleme dieser Periode** (de: "Probleme dieser Periode", en: "Issues in this
  period", fr: "Problèmes de cette période", it: "Problemi di questo periodo"), Anker
  `period-closing-issues`, initial zugeklappt (klappt beim Anspringen durch Klacksy auf):
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
- Bulk-Buttons unten: **Alle versiegeln** (de: "Alle versiegeln", en: "Seal all"; Anker
  `period-closing-seal-all`) — deaktiviert, wenn schon alle Tage versiegelt sind — und **Alle
  entsiegeln** (de: "Alle entsiegeln", en: "Unseal all"; Anker `period-closing-unseal-all`) —
  deaktiviert ohne versiegelte Tage, öffnet denselben Begründungs-Dialog. Beide Buttons
  erscheinen erst, wenn eine Periode gewählt und geladen ist. Vor dem Versiegeln fragt die Seite
  immer nach (Bestätigungs-Dialog); bleiben Fehler in der Periode offen, folgt eine zweite
  Rückfrage **Trotzdem abschliessen** (en: "Close anyway"). Erfolg wird als Toast gemeldet: "{count} Einträge versiegelt." bzw.
  "{count} Einträge wieder geöffnet."

### Karte **Exporte** (de: "Exporte", en: "Exports", fr: "Exports", it: "Esportazioni") — Anker `period-closing-exports`

Oben eine Tab-Leiste mit **drei Unter-Tabs**; es ist immer nur einer sichtbar, Standard ist der
erste. Welche Formate in den Dropdowns stehen, hängt von der Einstellung "Export-Formate" ab:
**CSV, JSON und XML sind immer da**, alle anderen nur, wenn sie dort aktiviert sind.

1. **Einzelne Bestellung** (de: "Einzelne Bestellung", en: "Single Order", fr: "Commande unique",
   it: "Ordine singolo"; Anker `period-closing-export-tab-single`) — Leistungsnachweis einer
   versiegelten Bestellung:
   - Datumsfilter **Von / Bis** (de: "Von"/"Bis", en: "From"/"Until", fr: "Du"/"Au", it:
     "Dal"/"Al"; Anker `period-closing-export-single-filter`, DOM-IDs
     `exportsFilterFrom`/`exportsFilterUntil`); Standard ist der 1. des Vormonats bis zum Ende
     des aktuellen Monats, daneben ein Aktualisieren-Button.
   - Suchfeld **Bestellung** (de: "Bestellung", en: "Order", fr: "Commande", it: "Ordine";
     Anker `period-closing-export-single-order`, DOM-ID `exportsSearch`), Placeholder "Kürzel,
     Name, Kunde, Kundennummer…": sucht versiegelte Bestellungen im Zeitfenster (300 ms
     Tipp-Verzögerung); Treffer erscheinen als Dropdown im Format "Kürzel – Name – Kunde –
     Zeitraum – geschlossen/gesamt"; ohne Treffer: "Keine Bestellungen im gewählten Zeitfenster
     gefunden."
   - Dropdown **Format** (Anker `period-closing-export-single-format`, DOM-ID `exportFormat`):
     die aktivierten Bestell-Formate — CSV, JSON, XML, DATEV, BMD NTCS, MOVEIN (IL), SIE 4B (SE),
     OMEGA (SK), Temeljnica (HR/SI).
   - Button **Exportieren** (de: "Exportieren", en: "Export", fr: "Exporter", it: "Esporta";
     Anker `period-closing-export-single-export`): deaktiviert ohne gewählte Bestellung; lädt
     den Leistungsnachweis direkt als Datei im Browser herunter (Erfolgs-Toast "Export erzeugt:
     {file}"). Sind nicht alle Schichten geschlossen, erscheint vorher die Warnung "X von Y
     Schichten sind geschlossen. Nicht geschlossene Schichten werden NICHT exportiert." (en: "...
     Non-closed shifts will NOT be exported."). Die Export-Sprache ist die aktuelle UI-Sprache;
     der Server legt nach erfolgreichem Download automatisch einen Eintrag im Export-Protokoll an.
2. **Mitarbeiterstunden** (de: "Mitarbeiterstunden", en: "Employee Hours", fr: "Heures des
   employés", it: "Ore dipendenti"; Anker `period-closing-export-tab-employee`) — Stunden, Spesen
   und Pausen aller Mitarbeitenden (intern und extern) für einen Zeitraum, unabhängig von
   einzelnen Bestellungen; Titel "Stunden/Spesen/Pausen pro Mitarbeiter":
   - Zeile mit **Von / Bis** (DOM-IDs `clientExportFrom`/`clientExportUntil`), **Format** und
     Export-Button (Anker der Zeile `period-closing-export-employee-form`).
   - Dropdown **Format** (Anker `period-closing-export-employee-format`, DOM-ID
     `clientExportFormat`): die generischen Formate XML/JSON/CSV (alle Mitarbeitenden) plus die
     aktivierten **Lohn-Formate**: DATEV Lohn & Gehalt (Bewegungsdaten), Generic CSV/Excel (Payroll),
     Merit Palk (EE), PAXml (SE), AbaConnect (CH), POHODA (CZ), WinMENTOR (RO), BrightPay
     (IE/UK).
   - Dropdown **Gruppe** (de: "Gruppe", en: "Group"; Anker `period-closing-export-employee-group`,
     DOM-ID `clientExportGroup`): erscheint **nur bei einem Lohn-Format** und ist dann Pflicht —
     ohne Gruppe bleibt der Export-Button gesperrt. Bei XML/JSON/CSV entfällt die Gruppe.
   - Button **Exportieren** (Anker `period-closing-export-employee-export`).
3. **Bestellungen im Zeitraum** (de: "Bestellungen im Zeitraum", en: "Orders in Range", fr:
   "Commandes sur la période", it: "Ordini nel periodo"; Anker
   `period-closing-export-tab-range`) — ERP-Zeitraum-Export (Titel "ERP-Zeitraum-Export"): alle
   versiegelten Bestellungen mit abgeschlossenen Diensten im Zeitraum als **ZIP** — eine Datei
   pro Bestellung plus ein Mitarbeiter-Perioden-XML:
   - Zeile mit **Von / Bis** (DOM-IDs `rangeExportFrom`/`rangeExportUntil`), **Format**, Laden-
     und ZIP-Button (Anker der Zeile `period-closing-export-range-form`).
   - Dropdown **Format** (Anker `period-closing-export-range-format`, DOM-ID `rangeExportFormat`):
     die aktivierten Bestell-Formate (Standard XML) für die Dateien im ZIP.
   - Button **Bestellungen laden** (Aktualisieren-Symbol) füllt die Tabelle **BESTELLUNG |
     KUNDE | ERP-REFERENZ | DIENSTE (GESCHLOSSEN/GESAMT)** mit Status je Bestellung
     (**Exportierbar**, **Periode nicht abgeschlossen**, **Nicht geschlossen**); pro Zeile lässt
     sich ein Detail (Datum, Mitarbeitende, Zeit, Stunden, Zuschläge, Status) auf- und zuklappen.
   - Button **ZIP exportieren** (de: "ZIP exportieren", en: "Export ZIP"; Anker
     `period-closing-export-range-export`): erst aktiv, wenn Bestellungen geladen sind. Ohne
     Treffer: "Keine versiegelten Bestellungen im gewählten Zeitraum."

Ein Anker in einem gerade nicht sichtbaren Unter-Tab schaltet den Tab automatisch um — die
Anker der Unter-Tabs (`...-tab-single`, `...-tab-employee`, `...-tab-range`) sind immer sichtbar.

### Karte **Protokoll** (de: "Protokoll", en: "Audit Log", fr: "Journal d'audit", it: "Registro di controllo") — Anker `period-closing-audit`

- Datumsfilter **Startdatum / Enddatum** (de: "Startdatum"/"Enddatum", en: "Start date"/
  "End date"; Anker `period-closing-audit-filter`), DOM-IDs `auditStartDate`/`auditEndDate`;
  Standard ist der aktuelle Monat, daneben ein Aktualisieren-Button.
- Aufklappbare Sektion **Aktionen** (de: "Aktionen", en: "Actions", fr: "Actions", it:
  "Azioni"; Anker `period-closing-audit-actions`) mit Tabelle `audit-log-table`, Spalten
  **DATUM | AKTION | BENUTZER | BETROFFENE EINTRÄGE** (en: DATE | ACTION | USER | AFFECTED
  ENTRIES); die Aktion ist ein Badge: **Versiegelt** (en: "Sealed"), **Wieder geöffnet** (en:
  "Reopened"), **Tag genehmigt** (en: "Day approved"), **Arbeitszeit bestätigt** (en: "Work
  confirmed"), **Pause bestätigt** (en: "Break confirmed"); eine nicht zuordenbare Aktion heisst
  **Unbekannte Aktion**. Klick (oder Enter) auf eine Zeile öffnet das Modal **Protokoll-Eintrag** (de:
  "Protokoll-Eintrag", en: "Audit Entry") mit Aktion, Ausgeführt am/von, Zeitraum, Gruppe
  (oder **Alle Gruppen**, en: "All groups"), Betroffene Einträge und Begründung.
- Aufklappbare Sektion **Exporte** (en: "Exports"; Anker `period-closing-audit-exports`) mit
  Tabelle `export-log-table`, Spalten
  **ERZEUGT AM | FORMAT | DATEI | BENUTZER | EINTRÄGE** (en: CREATED AT | FORMAT | FILE |
  USER | RECORDS). Klick auf eine Zeile öffnet das Modal **Export-Eintrag** (de:
  "Export-Eintrag", en: "Export Entry") mit Format, Erzeugt am/von, Zeitraum, Gruppe,
  Dateiname, Grösse (formatiert in B/KB/MB/GB), Anzahl Einträge, Sprache und Währung.
- Leere Zeiträume zeigen "Keine Einträge im ausgewählten Zeitraum." bzw. "Keine Exporte im
  ausgewählten Zeitraum."

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Was Versiegeln tut**: Das Versiegeln setzt alle Dienst- und Absenz-Einträge
  des Zeitraums auf die höchste der vier Sperrstufen (Offen → Bestätigt → Freigegeben →
  **Abgeschlossen**) und legt zusätzlich pro Kalendertag ein **Tagessiegel** an — mit Gruppenbezug,
  wenn die gewählte Periode eine Gruppe trägt, sonst global für alle. Der gemeldete Zähler
  "betroffene Einträge" ist die Summe aus versiegelten Dienst- und Absenz-Einträgen plus neu
  angelegten Tagessiegeln. Der Protokoll-Eintrag wird in derselben Transaktion geschrieben.
- **Wirkung des Tagessiegels**: Für versiegelte Tage lehnt das Backend jede Neuanlage,
  Änderung und Löschung von Dienst-Einträgen, Absenzen, Spesen und Korrekturen ab —
  mit der Fehlermeldung **Tag ist gesperrt und kann nicht geändert werden** (de: "Tag ist
  gesperrt und kann nicht geändert werden", en: "Day is sealed and cannot be modified", fr:
  "Le jour est scellé et ne peut être modifié", it: "Il giorno è sigillato e non può essere
  modificato"). Ein Tagessiegel **ohne** Gruppe sperrt den Tag für alle; ein Tagessiegel
  **mit** Gruppe sperrt nur Mitarbeitende, deren Buchung an diesem Tag über einen Dienst zu dieser
  Gruppe gehört.
- **Szenario-Isolation**: Einträge aus Analyse-Szenarien (des
  Planungs-Assistenten) umgehen die Tagessperre — die Prüfung wird für Szenario-Einträge
  übersprungen; verbindlich wird die Sperre erst beim Übernehmen ins echte Schedule. Auch die
  Problemliste berücksichtigt nur reale Planungs-Notizen (keine Szenario-Notizen).
- **Entsiegeln**: verlangt zwingend eine Begründung (das Backend lehnt ohne Begründung ab),
  setzt die Sperrstufe der Dienst- und Pausen-Einträge auf die unterste Stufe zurück und löscht die Tagessiegel (sie wandern in den
  Papierkorb); auch dies schreibt einen Protokoll-Eintrag (Wieder geöffnet) mit der Begründung.
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
- **Seitenkontext**: Die auf der Perioden-Karte gewählte Periode (Start/Ende) und deren Gruppe
  kennt Klacksy: "diese Periode" / "diese Gruppe" muss der Benutzer nicht nochmals nennen. Beim
  Verlassen der Seite wird der Bezug zurückgesetzt.
- **Schichtplan (`/workplace/schedule`)**: Dort entstehen die Work-Einträge, die hier
  versiegelt werden; Änderungen an versiegelten Tagen schlagen dort mit der
  Tagessperre-Fehlermeldung fehl.
- **Absenzen Kalender (`/workplace/absence`)**: Neuanlage und Änderung von Absenzen in
  versiegelten Zeiträumen werden vom Backend ebenso abgelehnt — erst entsiegeln, dann
  korrigieren.

### Automatischer Abschluss

Klacksy kann eine Periode **selbständig abschliessen**, aber nur wenn **alle** diese Bedingungen
erfüllt sind (Standard: keine davon ist eingeschaltet, also schliesst Klacksy nie selbst ab):

- Im Klacksy-Handlungsspielraum (Einstellungen) steht die Regel **"Automatischer
  Periodenabschluss"** auf **"Ausführen"** (en: "Automatic period close" / "Carry out"). Ab Werk
  steht sie auf "Nur melden"; sie kann auch nur für einzelne Gruppen gesetzt werden.
- Die globale Autonomie-Stufe ist **Voll autonom** (de: "Voll autonom", en: "Fully autonomous").
- **Alle** Admins haben die Autonomie-Stufe **Voll autonom** gewählt — es zählt das Minimum
  aller Admins; ein einziger Admin auf einer tieferen Stufe oder ohne gewählte Stufe verhindert den
  automatischen Abschluss für alle. Der Notaus (Kill-Switch) ist aus.
- Der **Abschluss-Zeitpunkt** wurde im Gespräch festgelegt: die Zahl der Tage nach dem
  Periodenende, an denen abgeschlossen wird (0 = direkt am Tag nach dem Ende). Solange dieser
  Zeitpunkt nicht gespeichert ist, schliesst Klacksy **nie** automatisch — der gespeicherte Wert
  ist der Nachweis, dass gefragt wurde. Wenn er fehlt, frage nach: **"Wann soll die Periode
  abgeschlossen werden?"** Speichern läuft über den Skill `set_period_close_lag` (braucht deine
  Bestätigung); den Zeitpunkt je Gruppe und ob Klacksy dort selbst abschliesst (mit dem Grund,
  falls nicht) zeigt `get_period_close_schedule`.
- Die Periode ist vollständig vorbei, der Abschluss-Termin erreicht (höchstens 3 Tage zurück),
  die Gruppe hat in ihren **eigenen** Schichten Arbeit im Zeitraum, noch kein Tag ist versiegelt,
  die Periode wurde nie wieder geöffnet und in der Problemliste steht kein **Fehler**.
  Abgeschlossen wird immer pro Gruppe, nie global. Bei Gruppen mit Zahlungsintervall
  "Individuell" gibt es keinen festen Zyklus — dort schliesst Klacksy nie automatisch.

Solange die ersten drei Bedingungen für eine Gruppe nicht erfüllt sind, ist der automatische
Abschluss dort nicht eingeschaltet: Klacksy schliesst nicht ab und erzeugt dazu **keine** Meldung —
es kommen nur die üblichen Erinnerungen ("Periode endet bald" / "Periode überfällig"), die sich
nach dem Abschluss-Termin richten. Ist er eingeschaltet, ist die Erinnerung drei Tage vorher
zugleich die **Vorankündigung** — bis dahin kann der Admin widersprechen. Scheitert dann eine
der übrigen Bedingungen, schliesst Klacksy nicht und meldet den Grund (z. B. kein
Abschluss-Zeitpunkt gespeichert, offene Fehler, teilweise versiegelt, Termin zu lange her).
Eine Periode, die jemand wieder geöffnet hat, schliesst Klacksy nie erneut selbst ab. Jeder
automatische Abschluss steht mit dem entscheidenden Admin und dem Handelnden "Klacksy
(autonom)" im Protokoll (Aktion **Versiegelt**) und wird gemeldet. Ein Abschluss ist **nicht
verlustfrei umkehrbar**: Wieder öffnen stellt bestätigte/freigegebene Zwischenstufen nicht wieder
her, und bei einer Gruppe mit Lohn-/ERP-Übergabe löst der Abschluss den Export aus.

### Typische Aufgaben

- Welche Perioden sind abschlussreif? → Perioden-Dropdown; per Chat: `list_open_periods`
  (verbuchte Perioden inkl. Siegel-Status, neueste zuerst).
- Vor dem Abschluss offene Probleme prüfen (Fehler/Warnungen/unverplante Shifts) → Skill
  `list_period_issues` (Vorprüfung — VOR `close_period` aufrufen); Kurzübersicht:
  `generate_period_summary`.
- Monat zum Abrechnungsende versiegeln → Periode wählen, Probleme-Karte prüfen, dann
  **Alle versiegeln** — oder per Klacksy-Skill `close_period` (gruppen-bewusst: mit Gruppe
  entstehen Tagessiegel, Audit-Eintrag UND die Lohn-/ERP-Übergabe; ohne Gruppe global,
  dann feuert KEIN Export; DB-verifiziert).
- Ist die Periode versiegelt? Wie viele Tage offen? → Badge-Zeile; per Chat:
  `get_period_status` (versiegelt/teilweise/offen/leer, nennt offene Tage).
- Periode wieder öffnen → Skill `reopen_period` (Begründung PFLICHT, landet im Protokoll;
  warnt, dass bestehende Exporte danach nicht mehr stimmen); Tage freigeben/Freigabe
  zurücknehmen vor dem Abschluss: `approve_day` / `revoke_day_approval`.
- Leistungsnachweis für einen Kunden exportieren → Bestellung suchen, Format wählen,
  **Exportieren** — Skills: `list_sealed_orders`, `open_order_export`.
- Nachvollziehen, wer wann versiegelt/geöffnet oder exportiert hat → Protokoll-Karte; per
  Chat: `list_period_audit_log` (Siegel-Protokoll mit Begründungen) und
  `list_recent_exports` (Export-Historie).
- Wann eine Periode automatisch abgeschlossen wird → `get_period_close_schedule`; den Zeitpunkt
  festlegen → im Gespräch klären ("Wann soll die Periode abgeschlossen werden?"), dann
  `set_period_close_lag`.
- Zur Seite springen — Skill `navigate_to` (Ziel "period-closing"; Unterziele wie
  `period-closing-export-tab-employee` oder `period-closing-issues`).

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
