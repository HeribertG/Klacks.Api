# Macro: Zuschlagsberechnung

## Übersicht

Dieses Macro berechnet Zuschlagsstunden basierend auf:
- **Nachtdienst** (23:00-06:00): 10%
- **Feiertag**: 15%
- **Wochenende** (Sa/So): 10%

Bei mehreren anwendbaren Zuschlägen gilt der **höchste** Satz.

## Externe Variablen

| Variable | Typ | Beschreibung | Beispiel |
|----------|-----|--------------|----------|
| `Weekday` | Integer | Wochentag des Dienstbeginns (1=Mo, 2=Di, ..., 6=Sa, 7=So) | `7` |
| `FromHour` | String | Startzeit im Format "HH:MM" | `"22:00"` |
| `UntilHour` | String | Endzeit im Format "HH:MM" | `"07:00"` |
| `Hour` | Decimal | Gesamte Arbeitsstunden | `9.0` |
| `Holiday` | Integer | Feiertagsstatus (0=kein, 1=Feiertag, -1=Vortag, -2=Nachtag) | `1` |
| `WeekdayNextDay` | Integer | Wochentag des Folgetags (für Nachtschichten) | `1` |
| `HolidayNextDay` | Integer | Feiertagsstatus des Folgetags | `0` |

## Macro-Code

```vb
' Externe Variablen:
' Weekday, FromHour, UntilHour, Hour, Holiday
' WeekdayNextDay, HolidayNextDay (für Nachtschichten)

Dim TotalBonus

' Prüfe ob Schicht über Mitternacht geht
If TimeToHours(UntilHour) <= TimeToHours(FromHour) Then
    ' Schicht kreuzt Mitternacht - 2 Segmente
    TotalBonus = CalcSegment(FromHour, "00:00", Holiday, Weekday)
    TotalBonus = TotalBonus + CalcSegment("00:00", UntilHour, HolidayNextDay, WeekdayNextDay)
Else
    ' Normale Tagesschicht
    TotalBonus = CalcSegment(FromHour, UntilHour, Holiday, Weekday)
End If

Message(1, "Zuschlag: " & Round(TotalBonus, 2) & " Std")

Function CalcSegment(StartTime, EndTime, HolidayFlag, WeekdayNum)
    Dim SegmentHours
    Dim NightHours
    Dim NonNightHours
    Dim NightRate
    Dim NonNightRate
    Dim HasHoliday
    Dim HasWeekend

    ' Berechne Stunden im Segment
    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    If SegmentHours < 0 Then SegmentHours = SegmentHours + 24

    ' Teile in Nacht/Nicht-Nacht auf
    NightHours = TimeOverlap("23:00", "06:00", StartTime, EndTime)
    NonNightHours = SegmentHours - NightHours

    HasHoliday = HolidayFlag = 1
    HasWeekend = WeekdayNum = 6 OrElse WeekdayNum = 7

    ' Höchster Satz für Nachtstunden
    NightRate = 0
    If NightHours > 0 Then
        NightRate = 0.10
        If HasHoliday AndAlso 0.15 > NightRate Then NightRate = 0.15
        If HasWeekend AndAlso 0.10 > NightRate Then NightRate = 0.10
    End If

    ' Höchster Satz für Nicht-Nachtstunden
    NonNightRate = 0
    If HasHoliday AndAlso 0.15 > NonNightRate Then NonNightRate = 0.15
    If HasWeekend AndAlso 0.10 > NonNightRate Then NonNightRate = 0.10

    CalcSegment = NightHours * NightRate + NonNightHours * NonNightRate
End Function
```

## Berechnungslogik

### 1. Schicht-Erkennung
Das Macro erkennt automatisch ob eine Schicht über Mitternacht geht:
- `UntilHour <= FromHour` → Nachtschicht (z.B. 22:00-07:00)
- Sonst → Tagesschicht (z.B. 08:00-16:00)

### 2. Segment-Aufteilung bei Nachtschichten
Bei Nachtschichten wird die Arbeitszeit in zwei Segmente aufgeteilt:

| Segment | Zeitraum | Verwendet |
|---------|----------|-----------|
| 1 | FromHour bis 00:00 | `Holiday`, `Weekday` |
| 2 | 00:00 bis UntilHour | `HolidayNextDay`, `WeekdayNextDay` |

### 3. Nacht/Nicht-Nacht Aufteilung
Innerhalb jedes Segments wird weiter aufgeteilt:
- **Nachtstunden**: Überlappung mit 23:00-06:00
- **Nicht-Nachtstunden**: Restliche Stunden

### 4. Satz-Ermittlung
Für jede Stundengruppe wird der höchste anwendbare Satz ermittelt:

| Zuschlag | Satz | Bedingung |
|----------|------|-----------|
| Nacht | 10% | Stunden innerhalb 23:00-06:00 |
| Feiertag | 15% | `Holiday = 1` |
| Wochenende | 10% | `Weekday = 6` (Sa) oder `Weekday = 7` (So) |

## Verwendete Scripting-Funktionen

| Funktion | Beschreibung |
|----------|--------------|
| `TimeToHours("HH:MM")` | Konvertiert Zeit-String zu Dezimalstunden |
| `TimeOverlap("s1", "e1", "s2", "e2")` | Berechnet Überlappungsstunden zweier Zeiträume |
| `Round(zahl, dezimalen)` | Rundet auf n Dezimalstellen |
| `Message(typ, text)` | Gibt Ergebnis zurück |

## Beispielberechnungen

### Beispiel 1: Normale Tagesschicht
**Eingabe:** Mo 08:00-16:00, kein Feiertag
```
Weekday = 1
FromHour = "08:00"
UntilHour = "16:00"
Hour = 8
Holiday = 0
```
**Berechnung:**
- Keine Nachtschicht (16:00 > 08:00)
- Nachtstunden: 0
- Nicht-Nachtstunden: 8
- Kein Feiertag, kein Wochenende → Rate 0%

**Ergebnis:** `0.00 Std`

---

### Beispiel 2: Sonntagsschicht
**Eingabe:** So 08:00-16:00
```
Weekday = 7
FromHour = "08:00"
UntilHour = "16:00"
Hour = 8
Holiday = 0
```
**Berechnung:**
- Keine Nachtschicht
- Nachtstunden: 0
- Nicht-Nachtstunden: 8
- Wochenende → Rate 10%
- 8 × 0.10 = 0.80

**Ergebnis:** `0.80 Std`

---

### Beispiel 3: Feiertagsschicht
**Eingabe:** Mo 08:00-16:00, Feiertag
```
Weekday = 1
FromHour = "08:00"
UntilHour = "16:00"
Hour = 8
Holiday = 1
```
**Berechnung:**
- Keine Nachtschicht
- Nachtstunden: 0
- Nicht-Nachtstunden: 8
- Feiertag → Rate 15%
- 8 × 0.15 = 1.20

**Ergebnis:** `1.20 Std`

---

### Beispiel 4: Nachtschicht über Mitternacht
**Eingabe:** So 22:00 - Mo 07:00, Sonntag ist Feiertag
```
Weekday = 7
FromHour = "22:00"
UntilHour = "07:00"
Hour = 9
Holiday = 1
WeekdayNextDay = 1
HolidayNextDay = 0
```
**Berechnung:**

**Segment 1: So 22:00-00:00** (Holiday=1, Weekday=7)
| Stunden | Typ | Anwendbar | Rate |
|---------|-----|-----------|------|
| 22:00-23:00 | Nicht-Nacht | Feiertag 15%, Wochenende 10% | 15% |
| 23:00-00:00 | Nacht | Nacht 10%, Feiertag 15%, Wochenende 10% | 15% |

Segment 1 Bonus: 1 × 0.15 + 1 × 0.15 = **0.30 Std**

**Segment 2: Mo 00:00-07:00** (Holiday=0, Weekday=1)
| Stunden | Typ | Anwendbar | Rate |
|---------|-----|-----------|------|
| 00:00-06:00 | Nacht | Nacht 10% | 10% |
| 06:00-07:00 | Nicht-Nacht | keine | 0% |

Segment 2 Bonus: 6 × 0.10 + 1 × 0.00 = **0.60 Std**

**Ergebnis:** `0.90 Std`

---

### Beispiel 5: Nachtschicht ohne Feiertag
**Eingabe:** Mo 22:00 - Di 06:00
```
Weekday = 1
FromHour = "22:00"
UntilHour = "06:00"
Hour = 8
Holiday = 0
WeekdayNextDay = 2
HolidayNextDay = 0
```
**Berechnung:**

**Segment 1: Mo 22:00-00:00**
- Nachtstunden: 1 (23:00-00:00) × 10% = 0.10
- Nicht-Nachtstunden: 1 (22:00-23:00) × 0% = 0.00

**Segment 2: Di 00:00-06:00**
- Nachtstunden: 6 × 10% = 0.60
- Nicht-Nachtstunden: 0

**Ergebnis:** `0.70 Std`

## Anpassungen

### Niedrigsten Satz verwenden
Ersetze `>` durch `<` in den Rate-Vergleichen:
```vb
' Statt:
If HasHoliday AndAlso 0.15 > NightRate Then NightRate = 0.15

' Verwende:
If HasHoliday AndAlso 0.15 < NightRate Then NightRate = 0.15
```

### Andere Zuschlagssätze
Passe die Prozentsätze an:
```vb
NightRate = 0.10      ' → z.B. 0.25 für 25%
' ...
If HasHoliday AndAlso 0.15 > Rate Then  ' → z.B. 0.50 für 50%
If HasWeekend AndAlso 0.10 > Rate Then  ' → z.B. 0.25 für 25%
```

### Andere Nachtzeit-Definition
Passe die TimeOverlap-Parameter an:
```vb
' Statt 23:00-06:00:
NightHours = TimeOverlap("22:00", "05:00", StartTime, EndTime)
```
