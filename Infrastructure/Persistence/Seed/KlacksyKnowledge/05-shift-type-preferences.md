---
name: explain_shift_type_preferences
description: |
  Explains Diensttypwünsche (planning commands): per-day alias keywords FREE / EARLY / LATE / NIGHT
  and their negations -EARLY / -LATE / -NIGHT; these are the English defaults, admins can rename
  them in Settings. They are strong wishes: the automatic planning first plans strictly; if shifts
  stay open, a second run may break a wish to fill them (FREE before only-X before no-X). -FREE and
  EARLY/LATE/NIGHT also attract a shift. Use this when the user asks how to steer the automatic
  planning or what -EARLY etc. means.
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

Ein Diensttypwunsch ist eine kurze Vorgabe pro Tag und Mitarbeiter, die der automatischen Planung als starker Wunsch sagt, welche Dienstarten diese Person an dem Tag bekommen soll; in der Oberfläche heisst er „Planungsbefehl“.

<!-- level:short -->

## Wozu es Diensttypwünsche gibt

Manchmal soll jemand an einem bestimmten Tag gar nicht arbeiten oder nur eine bestimmte Art von Dienst bekommen, zum Beispiel nur Frühdienst, weil die Person abends Kinder betreut. Für solche Fälle schreibt der Planer eine kurze Vorgabe für diese Person und diesen Tag direkt in den Einsatzplan. Die automatische Planung hält sich zuerst streng daran. Nur wenn sonst Dienste offen blieben, darf ein zweiter Lauf einen Wunsch brechen, um diese Dienste zu besetzen. Von Hand kannst du trotzdem anders planen; Klacks meldet dann einen Konflikt, verbietet es aber nicht. Beispiel: Steht bei Anna am Mittwoch „nur Frühdienst“, schlägt die automatische Planung ihr an diesem Tag keinen Spät- und keinen Nachtdienst vor.

<!-- level:elements -->

## Vorgaben eintragen, Schritt für Schritt

1. **Befehle sichtbar machen.** Öffne den Einsatzplan (de: "Alle Planungen", en: "All Schedules", fr: "Tous les plannings", it: "Tutte le pianificazioni") in der Tabellenansicht und schalte in der Werkzeugleiste (de: "Befehle anzeigen", en: "Show Commands", fr: "Afficher les commandes", it: "Mostra comandi") ein. Ausgeschaltet bleiben die Zellen mit einer Vorgabe leer, die Vorgabe gilt aber trotzdem.
2. **Eintragen.** Tippe das Kürzel direkt in die Zelle der Person und des Tages und bestätige mit Enter. Welche Kürzel es gibt, steht in den Einstellungen: Abschnitt (de: "Arbeitszeit & Planung", en: "Work & Scheduling"), Karte (de: "Planungsregeln (Standardwerte)", en: "Scheduling Rules (Defaults)"), Gruppe (de: "Planungsbefehle (Keywords)", en: "Schedule Command Keywords").
3. **Ändern oder löschen.** Tippe in die Zelle mit der Vorgabe ein anderes Kürzel, oder öffne das Kontextmenü der Zelle mit (de: "Bearbeiten...", en: "Edit...") und (de: "Löschen", en: "Delete").
4. **Planen lassen.** Die automatische Planung (der Wizard-Knopf der Werkzeugleiste; ihn darf jeder benutzen, der den Plan bearbeiten darf, also auch ein Planer ohne Rolle) schreibt ihr Ergebnis als Szenario. Im strengen Plan sind die Vorgaben eingehalten; bietet sie zusätzlich einen zweiten Plan an, zeigt dessen Zusammenfassung, welche Wünsche er bricht.

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
| `-FREE` | (de: "Möglichst Arbeit (-FREE)", en: "Prefer work (-FREE)") | schränkt keine Dienstart ein, wünscht aber einen Dienst |
| `-EARLY` | (de: "Keine Frühdienste (-EARLY)", en: "No early shift (-EARLY)") | alles ausser Frühdienst |
| `-LATE` | (de: "Keine Spätdienste (-LATE)", en: "No late shift (-LATE)") | alles ausser Spätdienst |
| `-NIGHT` | (de: "Keine Nachtdienste (-NIGHT)", en: "No night shift (-NIGHT)") | alles ausser Nachtdienst |

### Was ein Früh-, Spät- oder Nachtdienst ist

Die Dienstart bestimmt Klacks aus der ganzen Zeitspanne des Dienstes: Früh 06:00 bis 17:00, Spät 17:01 bis 22:59, Nacht 23:00 bis 05:59 (Owner-Entscheid 2026-11-11; vorher Früh bis 14:59, Spät ab 15:00). Klacks zählt die Arbeitsminuten je Band; das stärkere Band gewinnt (Nacht vor Spät vor Früh), aber ein kurzes Hineinragen wird toleriert: bis 60 Minuten und weniger als die Hälfte des Dienstes (Toleranz 60 Minuten, Owner-Entscheid 2026-11-11). Beispiele: 08:00 bis 16:00 ist Früh (bis 2026-11-11 war er Spät), 09:00 bis 18:00 ist Früh (59 Minuten im Spät-Band), 09:00 bis 18:02 ist Spät (61 Minuten), 13:00 bis 19:00 ist Spät, 05:00 bis 13:00 ist Früh (genau 60 Minuten Nacht), 15:00 bis 00:30 ist Nacht. Die Toleranz gilt auch für die Nacht: 05:00 bis 13:00 ist Früh, 14:00 bis 00:00 ist Spät. Folge für die Wünsche: Ein Dienst bis 18:00 Uhr zählt als Frühdienst und kann deshalb jemandem mit `-LATE` (keine Spätdienste) zugeteilt werden, bei `-EARLY` oder `LATE` wird er abgelehnt. `-NIGHT` (keine Nachtdienste) erlaubt jetzt 05:00 bis 13:00 und 14:00 bis 00:00; `NIGHT` (nur Nachtdienste) wird von 05:00 bis 13:00 nicht mehr erfüllt. Zuschläge, Ruhezeiten und Höchststunden rechnen unverändert mit den echten Uhrzeiten. Die Dienstart wird nicht gespeichert, sondern bei jedem Lauf aus den Zeiten bestimmt; ein bestehendes `LATE` oder `-EARLY` an einem Tag mit einem Dienst 08:00 bis 16:00 oder 09:00 bis 18:00 wirkt deshalb seit dieser Änderung anders.

### Wer die Vorgabe beachtet

- Die automatische Planung behandelt sie als starke Wünsche: Der strenge Lauf hält sie ein, ein zweiter Lauf darf sie nur brechen, um sonst offene Dienste zu besetzen. Die Verbesserungsstufen (Harmonizer) brechen nie einen Wunsch.
- Die Ersatzsuche schliesst einen Kandidaten aus, dessen Tag den Dienst nicht erlaubt.
- Klacksy lehnt das Platzieren eines Dienstes gegen die Vorgabe ab.
- Planst du von Hand im Raster gegen die Vorgabe, wird der Dienst gespeichert und als Fehler gemeldet.

### Verfügbarkeit bleibt hart

Eine Vorgabe überstimmt die Verfügbarkeit der Person nicht: Auch an einem Tag mit Vorgabe plant die automatische Planung nur in den verfügbaren Stunden. Nur ein Tag mit gebuchter Absenz hebt die Verfügbarkeit auf. Qualifikationsgrenzen bleiben bestehen.

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

**Ein starker Wunsch, kein Verbot (seit v1.0.60).** Die automatische Planung plant zuerst streng: Niemand wird gegen seine Vorgabe eingesetzt. Bleiben danach Dienste offen und gibt es einschränkende Vorgaben, rechnet ein zweiter Lauf, in dem die Vorgaben nur Wünsche sind. Sein Plan zählt nur, wenn er mehr Dienste besetzt. Bricht er dabei keinen Wunsch, ersetzt er den strengen Plan. Bricht er Wünsche, wird er als zweites Szenario angeboten, und die Zusammenfassung nennt die gebrochenen Wünsche mit Namen; die automatische Übernahme der Folgeperiode nimmt einen solchen Plan nie.

**Ein Wunsch bricht nur für einen sonst offenen Dienst, nie für Stunden.** Die Rangfolge ist strikt: `FREE` vor „nur X“ vor „kein X“. Ein gebrochenes `FREE` wiegt schwerer als beliebig viele gebrochene „nur X“, ein „nur X“ schwerer als beliebig viele „kein X“. Muss ein Wunsch brechen, trifft es den leichtesten. Die Einschränkungen gehen den garantierten Stunden vor; nur die anziehenden Wünsche (siehe unten) weichen den garantierten Stunden.

**`-FREE`, `EARLY`, `LATE` und `NIGHT` ziehen an.** `-FREE` ohne Dienst und `EARLY`/`LATE`/`NIGHT` ohne Dienst dieser Art zählen als unerfüllter Wunsch; die automatische Planung versucht, sie zu erfüllen, ohne dafür bereits erreichte garantierte Stunden zu opfern. Erzwungen wird kein Dienst: `NIGHT` heisst „wenn ein Dienst, dann Nacht“.

**Mehrere Vorgaben an einem Tag wirken zusammen.** Beim Tippen hält eine Zelle nur eine Vorgabe, Klacksy kann aber mehrere für einen Tag speichern. Die Erlaubnisse werden dann geschnitten: `EARLY` und `LATE` zusammen erlauben nichts mehr und wirken wie `FREE`.

**Tippen deutet die Eingabe in fester Reihenfolge.** Zuerst wird sie als Dienstkürzel dieses Tages gelesen (das bucht den Dienst), dann als Absenzkürzel, dann als Vorgabe. Passt nichts, entsteht eine Notiz; ein Tippfehler wird also zur Notiz und nicht zur Vorgabe. Trägt die Zelle schon eine Vorgabe und du tippst etwas Unbekanntes, passiert nichts.

**Umbenannte Kürzel verwaisen bestehende Vorgaben.** Gespeichert ist der Kürzeltext. Wer in den Einstellungen ein Kürzel umbenennt, lässt bestehende Vorgaben mit dem alten Kürzel ohne Meldung unwirksam werden. Zwei Kürzel mit demselben Text sind nicht auseinanderzuhalten, nur eine der beiden Bedeutungen ist erreichbar.

**Versteckte Vorgaben wirken.** Bei ausgeschaltetem Schalter (de: "Befehle anzeigen", en: "Show Commands") sieht die Zelle leer aus, die Vorgabe gilt weiter.

**Eine Absenz am selben Tag gewinnt ohnehin.** Ein gebuchter Absenztag sperrt die Person für die ganze Planung des Tages, ob mit oder ohne Vorgabe.
