---
name: explain_page_period_closing
description: |
  Explains the Period Closing page (Periodenabschluss) at /workplace/period-closing: the
  Periods card with the day-by-day seal table and bulk seal/unseal, the issues card with
  error/warning/info badges and unstaffed-shift count, the Exports card for downloading
  order exports (CSV/JSON/XML/DATEV/BMD), and the Audit Log card with seal/unseal actions
  and export history. Use this when the user asks what they see on the Period Closing page,
  what the cards/columns/badges mean, how to seal or reopen a period, or how to work with it.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - period closing
  - periodenabschluss
  - periode
  - versiegeln
  - seal
  - unseal
  - export log
  - protokoll
  - audit
  - abschluss
synonyms:
  de: [periodenabschluss, was sehe ich hier, erkläre diese seite, was bedeutet diese karte, periode versiegeln, periode öffnen, exportprotokoll, abschluss-seite]
  en: [period closing, what do I see here, explain this page, what does this card mean, seal period, reopen period, export log, audit log]
  fr: [clôture de période, qu'est-ce que je vois ici, explique cette page, que signifie cette carte, sceller la période, rouvrir la période, journal d'audit]
  it: [chiusura periodo, cosa vedo qui, spiega questa pagina, cosa significa questa scheda, sigilla periodo, riapri periodo, registro di controllo]
---

# Periodenabschluss — die Seite /workplace/period-closing

## Zweck (1 Satz)

Auf dem **Periodenabschluss** (de: "Periodenabschluss", en: "Period Closing", fr: "Clôture de période", it: "Chiusura periodo") versiegeln Admins Abrechnungsperioden Tag für Tag, erzeugen Bestell-Exporte und sehen jede Versiegelung/Öffnung im Protokoll — erreichbar über das Navi-Icon `open-period-closing` (nur für Admins sichtbar) oder direkt unter `/workplace/period-closing`.

## Bereiche in Render-Reihenfolge

### 1. Karte **Perioden** (de: "Perioden", en: "Periods", fr: "Périodes", it: "Periodi")

- Dropdown **Periode** (de: "Periode", en: "Period", fr: "Période", it: "Periodo"): listet alle verbuchten Perioden, gruppiert nach Zahlungsintervall — **Wöchentlich / Zweiwöchentlich / Monatlich / Individuell** (en: Weekly/Biweekly/Monthly/Individual). Das Label zeigt den Zeitraum plus ggf. den Gruppennamen; monatliche Perioden werden auf den vollen Kalendermonat normalisiert. Daneben ein Aktualisieren-Button.
- Badge-Zeile über der Tabelle: Anzahl versiegelter Tage (z. B. "12/31 ✓"), Anzahl teilweise versiegelter Tage und Anzahl leerer Tage (ohne Arbeits-/Absenz-Einträge), jeweils bezogen auf alle Tage der Periode.
- Aufklappbare Tabelle **Tage der Periode** (de: "Tage der Periode", en: "Days of the period", fr: "Jours de la période", it: "Giorni del periodo") mit drei Spalten:
  - **VERSIEGELT / GESAMT** (en: "SEALED / TOTAL"): eine Checkbox pro Tag. Anhaken versiegelt den Tag sofort; Abhaken öffnet einen Inline-Dialog **Begründung** (en: "Reason") — ohne Begründung kann kein Tag entsiegelt werden.
  - **DATUM** (en: "DATE"): der Kalendertag.
  - **STATUS**: z. B. "3/5 Arbeit, 1/2 Absenz" — versiegelte/gesamte Work-Einträge und versiegelte/gesamte Absenz-Einträge des Tages.
- Karte **Probleme dieser Periode** (de: "Probleme dieser Periode", en: "Issues in this period", fr: "Problèmes de cette période", it: "Problemi di questo periodo"), initial zugeklappt:
  - Badges je Schweregrad: **Fehler / Warnung / Hinweis** (en: Error/Warning/Note) plus ein Badge "N unverplante Shifts" (en: "N unstaffed shifts") — berechnet als Bedarf (SumEmployees × Quantity) minus tatsächlich gebuchte Personen; ein "+" am Zähler bedeutet, dass nicht alle Shifts geladen wurden.
  - Darunter die Problemliste, nach Datum gruppiert, je Eintrag Mitarbeitername + Meldung.
  - PDF-Button (Tooltip "Probleme als PDF exportieren") exportiert die Problemliste als PDF.
- Bulk-Buttons unten: **Alle versiegeln** (en: "Seal all") und **Alle entsiegeln** (en: "Unseal all") — Entsiegeln verlangt ebenfalls eine Begründung. Erfolg wird als Toast gemeldet ("{count} Einträge versiegelt/wieder geöffnet").

### 2. Karte **Exporte** (de: "Exporte", en: "Exports", fr: "Exports", it: "Esportazioni")

- Datumsfilter **Von / Bis** (en: "From"/"Until"); Standard ist der 1. des Vormonats bis zum Ende des aktuellen Monats, daneben ein Aktualisieren-Button.
- Suchfeld **Bestellung** (de: "Bestellung", en: "Order", fr: "Commande", it: "Ordine") mit Placeholder "Kürzel, Name, Kunde, Kundennummer…": sucht versiegelte Bestellungen (SealedOrders) im Zeitfenster; Treffer erscheinen als Dropdown im Format "Kürzel – Name – Kunde – Zeitraum – geschlossen/gesamt".
- Dropdown **Format**: CSV, JSON, XML, DATEV, BMD NTCS.
- Button **Exportieren** (en: "Export"): lädt den Leistungsnachweis der gewählten Bestellung direkt als Datei im Browser herunter. Sind nicht alle Schichten geschlossen, erscheint die Warnung "X von Y Schichten sind geschlossen. Nicht geschlossene Schichten werden NICHT exportiert." Der Server legt nach erfolgreichem Download automatisch einen Eintrag im Export-Protokoll an. Der Export hängt an der SealedOrder — spätere Umbenennungen oder Schnitte der operativen Schicht verändern das Dokument nicht.

### 3. Karte **Protokoll** (de: "Protokoll", en: "Audit Log", fr: "Journal d'audit", it: "Registro di controllo")

- Datumsfilter **Startdatum / Enddatum** (en: "Start date"/"End date") plus Aktualisieren-Button.
- Aufklappbare Tabelle **Aktionen** (de: "Aktionen", en: "Actions", fr: "Actions", it: "Azioni") mit Spalten **DATUM | AKTION | BENUTZER | BETROFFENE EINTRÄGE**; die Aktion ist ein Badge **Versiegelt** oder **Wieder geöffnet** (en: Sealed/Reopened). Klick auf eine Zeile öffnet das Modal **Protokoll-Eintrag** mit Aktion, Ausgeführt am/von, Zeitraum, Gruppe, Anzahl betroffener Einträge und Begründung.
- Aufklappbare Tabelle **Exporte** (de: "Exporte", en: "Exports") mit Spalten **ERZEUGT AM | FORMAT | DATEI | BENUTZER | EINTRÄGE**. Klick auf eine Zeile öffnet das Modal **Export-Eintrag** mit Format, Erzeugt am/von, Zeitraum, Gruppe, Dateiname, Grösse, Anzahl Einträge, Sprache und Währung.

## Typische Aufgaben auf dieser Seite

- Monat zum Abrechnungsende versiegeln → Periode wählen, Probleme-Karte prüfen, dann **Alle versiegeln** — oder per Klacksy-Skill `close_period`.
- Einzelnen Tag nachträglich korrigieren → Checkbox abhaken + Begründung — oder Skill `reopen_period` (Tage freigeben vor dem Abschluss: `approve_day` / `revoke_day_approval`).
- Vor dem Abschluss offene Probleme prüfen (Fehler/Warnungen/unverplante Shifts) und als PDF sichern; eine Kurzübersicht liefert der Skill `generate_period_summary`.
- Leistungsnachweis für einen Kunden exportieren → Bestellung suchen, Format wählen, **Exportieren** — Skills: `list_sealed_orders`, `open_order_export`.
- Nachvollziehen, wer wann versiegelt/geöffnet oder exportiert hat → Protokoll-Karte; per Chat: `list_recent_exports`.
- Zur Seite springen: Skill `navigate_to` mit Ziel period-closing.

## Verwandte Seiten und Happen

- **Arbeitsplan** (`/workplace/schedule`): dort entstehen die Work-Einträge, die hier versiegelt werden; versiegelte Tage sind im Plan gesperrt ("Tag ist gesperrt und kann nicht geändert werden").
- `explain_shift_lifecycle_order_to_shift` — was eine SealedOrder ist und warum der Export an ihr hängt.
