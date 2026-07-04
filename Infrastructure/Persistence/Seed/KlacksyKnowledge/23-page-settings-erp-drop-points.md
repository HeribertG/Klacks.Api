---
name: explain_page_settings_erp_drop_points
description: |
  Explains the ERP order & customer import feature (Bestellungs- & Kunden-Import) in Klacks
  settings — the single-drop-point ("one mailbox") model for automatically importing orders
  (as SealedOrder shifts) from XML files delivered by an external ERP system. Covers the four
  cards: import schedule (cron expression, time zone, enable toggle, run-now button,
  last-polled/last-error status), manual upload (drag & drop an XML file), the file explorer
  (pending/processed/error tabs with retry and delete) and the ERP push access tokens for
  HTTPS delivery. Also covers the manual/handbook tab with the sample XML download. Use this
  when the user asks how to set up or use the ERP order import. Supports a level parameter:
  short (purpose only), elements (every card explained), effects (data flow, how orders
  become shifts, how tokens and the drop-zone relate).
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - erp import
  - erp drop point
  - bestellungsimport
  - kunden-import
  - order import
  - xml import
  - erp anbindung
  - drop zone erp
synonyms:
  de: [wie richte ich den erp import ein, was ist ein drop point, bestellungen automatisch importieren, xml datei hochladen erp, erp zugriffstoken, wie funktioniert der bestellungsimport]
  en: [how do i set up erp import, what is a drop point, automatically import orders, upload xml file erp, erp access token, how does the order import work]
  fr: [comment configurer l'import erp, qu'est-ce qu'un drop point, importer des commandes automatiquement, jeton d'accès erp]
  it: [come configuro l'import erp, cos'è un drop point, importare ordini automaticamente, token di accesso erp]
---

# Bestellungs- & Kunden-Import — Sektion "ERP" in den Einstellungen

<!-- level:short -->

## Stufe 1 — Wofür ist diese Sektion?

Die Sektion **Bestellungs- & Kunden-Import** (de: "Bestellungs- & Kunden-Import", en:
"Order & Customer Import", fr: "Importation commandes & clients", it: "Importazione
ordini & clienti") liegt unter `/workplace/settings` (Anker-ID `erp-drop-points`,
zwischen "Externe Dienste" und "Plugins") und konfiguriert den automatischen Import von
Bestellungen aus XML-Dateien, die ein externes ERP-System liefert. Klacks folgt dem
**Ein-Briefkasten-Modell**: es gibt genau einen Drop-Point ("Default"), keine Liste zum
Anlegen mehrerer Briefkästen. Jede importierte Bestellung wird als **Dienst
(`Shift`) im Status `SealedOrder`** angelegt oder aktualisiert — es gibt kein eigenes
Bestellungs-Datenmodell. Admin-only (Seite selbst ist Admin-only, siehe
`explain_page_settings_overview`).

<!-- level:elements -->

## Stufe 2 — Die Elemente im Detail

Die Karte `app-erp-drop-points` (Kopftitel de: "ERP Drop Points") hat zwei Reiter:

### Reiter "Import" (de: "Import", en: "Import", fr: "Importation", it: "Importazione")

Vier auf-/zuklappbare Karten:

1. **Import-Zeitplan** (de: "Import-Zeitplan", en: "Import schedule"):
   - **Cron-Ausdruck** (`erp-drop-points-schedule-cron`): Standard-5-Feld-Cron
     (Minute Stunde Tag Monat Wochentag); leer = Standardwert `0 * * * *` (stündlich).
     Ungültige Eingabe zeigt sofort einen Fehlertext (`erp-drop-points-schedule-cron-error`).
   - **Zeitzone** (`erp-drop-points-schedule-timezone`): IANA-Zeitzonen-Auswahl.
   - **Import aktiv** (`erp-drop-points-import-enabled`): Checkbox, schaltet den
     Drop-Point ein/aus — deaktiviert werden keine Dateien mehr abgeholt.
   - **Status-Zeile**: "Letzte Abfrage" (`last-polled-at`, oder "Noch nie abgefragt")
     und, falls vorhanden, "Letzter Fehler" (`last-error`) in Rot.
   - **Jetzt importieren** (`erp-drop-points-run-import-btn`): löst sofort einen
     Import-Lauf aus, statt auf den nächsten Zeitplan-Tick zu warten (Skill
     `trigger_erp_import_run`).
2. **Manueller Upload** (de: "Manueller Upload"): Drag-&-Drop-Zone
   (`erp-drop-points-upload-zone`) — XML-Datei hierher ziehen oder klicken, um die
   Dateiauswahl zu öffnen. Der Import startet danach automatisch (kein separater
   Auslöse-Klick nötig).
3. **Dateien im Drop-Point** (de: "Dateien im Drop-Point"): drei Reiter mit
   Live-Zählern — **Eingang** (`pending`, wartet auf Verarbeitung), **Verarbeitet**
   (`processed`) und **Fehler** (`error`). Jede Zeile zeigt Dateiname, Größe, Datum;
   der Fehler-Reiter zusätzlich den Ablehnungsgrund sowie **Erneut importieren**
   (verschiebt die Datei zurück nach Eingang) und **Löschen**. Ein Refresh-Icon
   aktualisiert die Liste manuell.
4. **ERP-Anbindung per Push (Zugriffstoken)** (de: "ERP-Anbindung per Push
   (Zugriffstoken)", standardmäßig eingeklappt): nur nötig, wenn ein externes
   ERP-System Dateien per HTTPS direkt anliefert (statt dass ein Mensch sie manuell
   hochlädt). Token sind ein **eigenes, isoliertes Token-Universum** (nicht die
   allgemeinen Personal-Access-Tokens) und haben immer ein Ablaufdatum.

### Reiter "Handbuch" (de: "Handbuch", en: "Manual")

Zeigt das mehrsprachige ERP-Import-Handbuch (`assets/docs/erp-import-manual/`) inklusive
XML-Referenztabelle, sowie den Knopf **Beispieldatei herunterladen**
(`erp-drop-points-download-sample-btn`) mit einer Beispiel-XML (zwei Bestellungen:
vollständig und minimal).

<!-- level:effects -->

## Stufe 3 — Wirkungen & Zusammenspiel mit anderen Seiten

- **Datenfluss**: eine importierte Bestellung wird per externer Referenz eindeutig
  identifiziert; taucht dieselbe Referenz erneut auf, wird die bestehende Bestellung
  aktualisiert (unversiegelt) oder — falls bereits versiegelt (`SealedOrder`) — durch
  eine neue Version ersetzt (Supersession-Kette). Kunden werden dabei automatisch
  angelegt oder anhand vorhandener Daten wiedererkannt.
- **Qualifikationen werden NICHT importiert** (bewusste Entscheidung) — der Planer
  ergänzt sie beim Versiegeln; ein ERP-Hinweis kann über das `Description`-Feld
  mitgeliefert werden.
- **Dateien werden archiviert, nie gelöscht**: nach Verarbeitung wandern sie nach
  `processed/` bzw. `error/`, unabhängig davon ob der Server als lokales Verzeichnis
  oder S3-kompatiblen Objektspeicher konfiguriert ist (für den Nutzer transparent).
- **Zeitplan wirkt global**, nicht pro Datei: ein ungültiger Cron-Ausdruck lässt den
  Import komplett stillstehen (kein automatischer Lauf mehr), deshalb validiert die
  Oberfläche den Ausdruck sofort.
- **Zielort der importierten Bestellungen**: Bestellungseingang / Alle Dienste (nicht
  diese Settings-Seite) — dort erscheinen sie als versiegelte Dienste zur weiteren
  Bearbeitung durch den Planer.
- **Berechtigung**: wie die gesamte Einstellungen-Seite Admin-only; die
  Zugriffstoken-Sektion ist zusätzlich sicherheitssensibel (Klartext-Token nur bei
  Erzeugung sichtbar).

### Typische Aufgaben

- Status abfragen (aktiv, Zeitplan, nächster Lauf, Anzahl Dateien) — Skill
  `get_erp_import_status`
- Import sofort auslösen, statt auf den Zeitplan zu warten — Skill
  `trigger_erp_import_run`
- Zeitplan (Cron-Ausdruck/Zeitzone) ändern — Skill `set_erp_import_schedule`
- Manuellen Upload, Zugriffstoken erzeugen/widerrufen und einzelne Dateien
  erneut importieren/löschen bedient der Benutzer direkt auf der Seite — dafür gibt
  es (bewusst, aus Sicherheitsgründen bei Token-Erzeugung) keine Klacksy-Skills.

### Verwandte Seiten

- **Einstellungen-Übersicht** (`/workplace/settings`) — siehe
  `explain_page_settings_overview` für die anderen zwölf Sektionen.
- **Bestellungseingang / Alle Dienste** — hier landen die importierten Bestellungen
  als versiegelte Dienste zur Weiterbearbeitung.

### Trigger-Phrasen

- "Wie richte ich den ERP-Import ein?"
- "Was ist ein Drop-Point?"
- "Wie importiere ich Bestellungen automatisch?"
- "How do I upload an ERP order XML file?"
- "Läuft der ERP-Import gerade?" / "Importiere jetzt"
