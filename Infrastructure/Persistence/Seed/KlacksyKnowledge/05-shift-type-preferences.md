---
name: explain_shift_type_preferences
description: |
  Explains Diensttypwünsche (planning commands): per-day alias keywords FREE / EARLY / LATE / NIGHT
  and their negations -EARLY / -LATE / -NIGHT; these are the English defaults, admins can rename
  them in Settings. The automatic planning treats them as a hard limit with no fallback to FREE: a
  shift nobody else may take stays open. -FREE (prefer work) restricts nothing. Use this when the
  user asks how to steer the automatic planning or what -EARLY etc. means.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - alias
  de:
    - diensttypwunsch
    - wunsch
  en:
    - shift type
    - preference
    - free
    - early
    - late
    - night
synonyms:
  de: [diensttypwunsch, schichtwunsch, alias, frei, früh, spät, nacht, möglichst arbeit]
  en: [shift type preference, preference, alias, free, early, late, night, wish]
  fr: [préférence de service, alias, libre, matin, après-midi, nuit, souhait]
  it: [preferenza turno, alias, libero, mattino, sera, notte, desiderio]
---

# Diensttypwünsche (Planungsbefehle) — die automatische Planung lenken

Ein Diensttypwunsch ist eine kurze Vorgabe pro Tag und Mitarbeiter, die der automatischen Planung verbindlich sagt, welche Dienstarten diese Person an dem Tag bekommen darf; in der Oberfläche heisst er „Planungsbefehl“.

<!-- level:short -->

## Wozu es Diensttypwünsche gibt

Manchmal soll jemand an einem bestimmten Tag gar nicht arbeiten oder nur eine bestimmte Art von Dienst bekommen, zum Beispiel nur Frühdienst, weil die Person abends Kinder betreut. Für solche Fälle schreibt der Planer eine kurze Vorgabe für diese Person und diesen Tag direkt in den Einsatzplan. Die automatische Planung hält sich daran und setzt die Person an dem Tag nur so ein, wie es die Vorgabe erlaubt. Von Hand kannst du trotzdem anders planen; Klacks meldet dann einen Konflikt, verbietet es aber nicht. Beispiel: Steht bei Anna am Mittwoch „nur Frühdienst“, schlägt die automatische Planung ihr an diesem Tag keinen Spät- und keinen Nachtdienst vor.

<!-- level:elements -->

## Vorgaben eintragen, Schritt für Schritt

1. **Befehle sichtbar machen.** Öffne den Einsatzplan (de: "Alle Planungen", en: "All Schedules", fr: "Tous les plannings", it: "Tutte le pianificazioni") in der Tabellenansicht und schalte in der Werkzeugleiste (de: "Befehle anzeigen", en: "Show Commands", fr: "Afficher les commandes", it: "Mostra comandi") ein. Ausgeschaltet bleiben die Zellen mit einer Vorgabe leer, die Vorgabe gilt aber trotzdem.
2. **Eintragen.** Tippe das Kürzel direkt in die Zelle der Person und des Tages und bestätige mit Enter. Welche Kürzel es gibt, steht in den Einstellungen: Abschnitt (de: "Arbeitszeit & Planung", en: "Work & Scheduling"), Karte (de: "Planungsregeln (Standardwerte)", en: "Scheduling Rules (Defaults)"), Gruppe (de: "Planungsbefehle (Keywords)", en: "Schedule Command Keywords").
3. **Ändern oder löschen.** Tippe in die Zelle mit der Vorgabe ein anderes Kürzel, oder öffne das Kontextmenü der Zelle mit (de: "Bearbeiten...", en: "Edit...") und (de: "Löschen", en: "Delete").
4. **Planen lassen.** Die automatische Planung (der Wizard-Knopf der Werkzeugleiste, nur für Administratoren sichtbar) schreibt ihr Ergebnis als Szenario; darin sind die Vorgaben eingehalten.

<!-- level:effects -->

## Welche Kürzel es gibt und wer sie beachtet

### Die acht Standard-Kürzel

Die Kürzel sind in den Einstellungen umbenennbar; die Bedeutung bleibt gleich. Die Beschriftungen der Felder dort:

| Kürzel | Beschriftung des Feldes | Wirkung an diesem Tag |
|---|---|---|
| `FREE` | (de: "Frei (FREE)", en: "Free (FREE)") | kein Dienst |
| `EARLY` | (de: "Nur Frühdienste (EARLY)", en: "Early shift only (EARLY)") | nur Frühdienste |
| `LATE` | (de: "Nur Spätdienste (LATE)", en: "Late shift only (LATE)") | nur Spätdienste |
| `NIGHT` | (de: "Nur Nachtdienste (NIGHT)", en: "Night shift only (NIGHT)") | nur Nachtdienste |
| `-FREE` | (de: "Möglichst Arbeit (-FREE)", en: "Prefer work (-FREE)") | schränkt keine Dienstart ein (siehe Folgen und Grenzfälle) |
| `-EARLY` | (de: "Keine Frühdienste (-EARLY)", en: "No early shift (-EARLY)") | alles ausser Frühdienst |
| `-LATE` | (de: "Keine Spätdienste (-LATE)", en: "No late shift (-LATE)") | alles ausser Spätdienst |
| `-NIGHT` | (de: "Keine Nachtdienste (-NIGHT)", en: "No night shift (-NIGHT)") | alles ausser Nachtdienst |

### Was ein Früh-, Spät- oder Nachtdienst ist

Die Dienstart bestimmt Klacks aus der ganzen Zeitspanne des Dienstes: Früh 06:00 bis 14:59, Spät 15:00 bis 22:59, Nacht 23:00 bis 05:59. Es gilt das stärkste Band, das der Dienst berührt: Nacht vor Spät vor Früh. Ein Dienst von 08:00 bis 16:00 zählt deshalb als Spätdienst.

### Wer die Vorgabe beachtet

- Die automatische Planung (Wizard und Harmonizer) behandelt sie als harte Grenze.
- Die Ersatzsuche schliesst einen Kandidaten aus, dessen Tag den Dienst nicht erlaubt.
- Klacksy lehnt das Platzieren eines Dienstes gegen die Vorgabe ab.
- Planst du von Hand im Raster gegen die Vorgabe, wird der Dienst gespeichert und als Fehler gemeldet.

### Vorrang vor der Verfügbarkeit

Ein Tag mit Vorgabe oder mit gebuchter Absenz überstimmt die Verfügbarkeit der Person. Qualifikationsgrenzen bleiben bestehen.

### Datenfluss

Die Vorgabe wird mit Person und Tag (und im Szenario mit dem Szenario) gespeichert, und zwar als der Kürzeltext, der in der Zelle steht. Bei jedem Lauf ordnet Klacks die Kürzel anhand der aktuellen Einstellungen den Bedeutungen zu, ohne Beachtung der Gross- und Kleinschreibung. An einem gesperrten Tag (Freigabe oder Periodenabschluss) lassen sich Vorgaben nicht anlegen, ändern oder löschen.

### Verwandte Skills

- `add_schedule_command` — eine Vorgabe für Person und Tag setzen
- `add_schedule_commands_range` — dieselbe Vorgabe für jeden Tag eines Zeitraums (höchstens 92 Tage, identische werden übersprungen)
- `explain_planning_assistant` — wie der Assistent die Vorgaben verarbeitet
- `explain_planning_sheets_modular` — wo die Vorgaben getragen werden

### Trigger-Phrasen

- "Was bedeutet `-EARLY` in der Planung?"
- "Wie sage ich dem Assistenten, dass jemand nur Nachtschichten will?"
- "Was ist `-FREE`?"
- "How do shift-type preferences work in Klacks?"
- "Comment exprimer un souhait de service ?"

<!-- level:pitfalls -->

## Folgen und Grenzfälle

**Es ist eine harte Grenze, kein Wunsch.** Die automatische Planung setzt die Person an dem Tag nicht gegen die Vorgabe ein. Einen Rückfall auf `FREE` gibt es nicht: Findet sich für den Dienst sonst niemand, bleibt er unbesetzt.

**`-FREE` tut in der Planung nichts.** Die Beschriftung „Möglichst Arbeit“ verspricht mehr. Das Kürzel schränkt keine Dienstart ein; seine einzige Wirkung ist, dass der Tag wie jeder Tag mit Vorgabe die Verfügbarkeit der Person überstimmt.

**Mehrere Vorgaben an einem Tag wirken zusammen.** Beim Tippen hält eine Zelle nur eine Vorgabe, Klacksy kann aber mehrere für einen Tag speichern. Die Erlaubnisse werden dann geschnitten: `EARLY` und `LATE` zusammen erlauben nichts mehr und wirken wie `FREE`.

**Tippen deutet die Eingabe in fester Reihenfolge.** Zuerst wird sie als Dienstkürzel dieses Tages gelesen (das bucht den Dienst), dann als Absenzkürzel, dann als Vorgabe. Passt nichts, entsteht eine Notiz; ein Tippfehler wird also zur Notiz und nicht zur Vorgabe. Trägt die Zelle schon eine Vorgabe und du tippst etwas Unbekanntes, passiert nichts.

**Umbenannte Kürzel verwaisen bestehende Vorgaben.** Gespeichert ist der Kürzeltext. Wer in den Einstellungen ein Kürzel umbenennt, lässt bestehende Vorgaben mit dem alten Kürzel ohne Meldung unwirksam werden. Zwei Kürzel mit demselben Text sind nicht auseinanderzuhalten, nur eine der beiden Bedeutungen ist erreichbar.

**Versteckte Vorgaben wirken.** Bei ausgeschaltetem Schalter (de: "Befehle anzeigen", en: "Show Commands") sieht die Zelle leer aus, die Vorgabe gilt weiter.

**Eine Absenz am selben Tag gewinnt ohnehin.** Ein gebuchter Absenztag sperrt die Person für die ganze Planung des Tages, ob mit oder ohne Vorgabe.
