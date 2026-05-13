---
name: explain_shift_sporadic
description: |
  Explains the Sporadic Shift in Klacks. A sporadic shift may only occur a
  limited number of times per fixed period (Week/Month/Quarter/Semester/Year/
  ContractualTerm). SumEmployees caps parallel bookings per day, Quantity caps
  distinct booked days per period. Once either cap is reached, the remaining
  days in the period are visually blocked (opacity 0.3, not draggable). Use
  this when the user asks why a shift is blocked, what SporadicScope means, or
  how SumEmployees and Quantity interact.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - sporadic
  - sporadisch
  - scope
  - quantity
  - sumemployees
  - bezugsperiode
  - blocked
  - gesperrt
synonyms:
  de: [sporadisch, gelegentlich, bezugsperiode, periode, periodisch, kapazität, sperrung]
  en: [sporadic, occasional, scope, period, capacity, blocked, locked]
  fr: [sporadique, occasionnel, période, capacité, bloqué]
  it: [sporadico, occasionale, periodo, capacità, bloccato]
---

# Sporadic Shift — periodisch begrenzte Häufigkeit

## Kern-Idee (1 Satz)

Ein **Sporadic Shift** findet in einer festen Periode (Woche/Monat/Quartal/
Semester/Jahr/Vertragslaufzeit) nur begrenzt oft statt — die Periode hat eine
Tages-Kapazität (`SumEmployees`) **und** eine Tages-Anzahl-Kapazität
(`Quantity`); übrige Tage werden gesperrt, sobald eine der beiden Schwellen
greift.

## Felder

| Feld | Wirkung |
|---|---|
| `IsSporadic = true` | Aktiviert die Periodenlogik. |
| `SporadicScope` | Periodenlänge: `Week`, `Month`, `Quarter`, `Semester`, `Year`, `ContractualTerm`. |
| `SumEmployees` | **Pro Tag** maximal so viele Buchungen parallel. |
| `Quantity` | **Pro Periode** maximal so viele verschiedene Buchungstage. |

Maximalbuchungen pro Periode = `SumEmployees × Quantity`.

## Drei Zell-Zustände

| Status | Bedingung | Visuell |
|---|---|---|
| **FREE** | weder Tag noch Periode voll | normal buchbar |
| **BOOKED** | Tag ist voll (`engaged ≥ SumEmployees`) | sealed |
| **BLOCKED** | Tag leer, aber Periode hat schon `Quantity` belegte Tage | opacity 0.3, nicht draggable, **nicht** im Header-Tooltip |

## Beispiel

`Scope=Week, SumEmployees=2, Quantity=3` →
maximal 3 verschiedene Tage à 2 Mitarbeiter = 6 Buchungen pro Woche.
Sind Mo+Mi+Fr je voll → Di+Do+Sa+So in dieser Woche sind **BLOCKED**.

## Konflikt-Verhalten beim Buchen

Backend (`PostCommandHandler.EnsureNoSporadicConflictAsync`) wirft HTTP 409 mit
sprechender Message, wenn:

- der Tag schon `SumEmployees` Buchungen hat **oder**
- die Periode `Quantity` verschiedene Buchungstage erreicht hat und der
  Buchungstag noch leer ist.

## Verwandte Skills

- `explain_shift_time_range` — Sporadic + TimeRange sind kombinierbar
- `explain_shift_container` — Container können IsSporadic sein
- `explain_planning_assistant` — der Assistent respektiert die Sperrungen

## Trigger-Phrasen

- "Was ist ein sporadischer Dienst?"
- "Warum ist mein Dienst gesperrt obwohl noch niemand gebucht ist?"
- "Wie wirkt SporadicScope=Week?"
- "What does sporadic shift mean in Klacks?"
- "Pourquoi ce service est-il bloqué cette semaine ?"
- "Cosa significa turno sporadico?"
