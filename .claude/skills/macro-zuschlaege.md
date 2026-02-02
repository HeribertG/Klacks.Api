# Macro: Zuschlagsberechnung

## Zuschlagssätze

| Zuschlag | Satz | Bedingung |
|----------|------|-----------|
| Nacht | 10% | 23:00-06:00 |
| Feiertag | 15% | `holiday = true` |
| Wochenende | 10% | Sa/So |

Bei mehreren Zuschlägen gilt der **höchste** Satz.

## Externe Variablen

| Variable | Typ | Beschreibung |
|----------|-----|--------------|
| `hour` | Decimal | Gesamte Arbeitsstunden |
| `fromhour` | Decimal | Startzeit (Dezimal, z.B. 22.5 = 22:30) |
| `untilhour` | Decimal | Endzeit (Dezimal) |
| `weekday` | Integer | ISO-8601 (1=Mo, 7=So) |
| `holiday` | Boolean | Feiertag heute |
| `holidaynextday` | Boolean | Feiertag morgen |

## Weekday-Werte (ISO-8601)

| Wert | Tag |
|------|-----|
| 1 | Montag |
| 2 | Dienstag |
| 3 | Mittwoch |
| 4 | Donnerstag |
| 5 | Freitag |
| 6 | Samstag |
| 7 | Sonntag |

## Berechnungslogik

### 1. Schicht-Erkennung
- `UntilHour <= FromHour` → Nachtschicht (über Mitternacht)
- Sonst → Tagesschicht

### 2. Segment-Aufteilung bei Nachtschichten

| Segment | Zeitraum | Variablen |
|---------|----------|-----------|
| 1 | FromHour bis 00:00 | Holiday, Weekday |
| 2 | 00:00 bis UntilHour | HolidayNextDay, WeekdayNextDay |

### 3. Satz-Ermittlung

Für jede Stundengruppe wird der höchste anwendbare Satz ermittelt.

## Beispiele

### Normale Tagesschicht (Mo 08:00-16:00)
- Keine Nachtstunden, kein Wochenende, kein Feiertag
- **Ergebnis:** 0.00 Std

### Sonntagsschicht (So 08:00-16:00)
- 8h × 10% (Wochenende)
- **Ergebnis:** 0.80 Std

### Feiertagsschicht (Feiertag 08:00-16:00)
- 8h × 15% (Feiertag)
- **Ergebnis:** 1.20 Std

### Nachtschicht (Mo 22:00 - Di 06:00)
- Segment 1: 1h Nacht (23:00-00:00) × 10%
- Segment 2: 6h Nacht (00:00-06:00) × 10%
- **Ergebnis:** 0.70 Std

## Speicherung

### Work.Surcharges

Macro-Ergebnis wird in `Work.Surcharges` gespeichert:

```csharp
// WorkMacroService.cs
work.Surcharges = macroResult;
```

### PeriodHours Berechnung

```csharp
TotalSurcharges = g.Sum(w => w.Surcharges)
Surcharges = workData.Surcharges + workChangeSurcharges
```

## Scripting-Funktionen

| Funktion | Beschreibung |
|----------|--------------|
| `TimeToHours("HH:MM")` | Zeit zu Dezimalstunden |
| `TimeOverlap("s1","e1","s2","e2")` | Überlappungsstunden |
| `Round(zahl, dezimalen)` | Runden |
| `Message(typ, text)` | Ergebnis zurückgeben |
