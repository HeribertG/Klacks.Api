---
name: explain_shift_type_preferences
description: |
  Explains Diensttypwünsche (shift-type preferences) in Klacks. Planners express
  per-day preferences for an employee using alias keywords FREE / EARLY / LATE /
  NIGHT and their negations (-FREE / -EARLY / -LATE / -NIGHT). The planning
  assistant honours them when possible; unfulfillable preferences default to
  FREE. Use this when the user asks how to bias the assistant, how aliases work,
  or what -EARLY etc. means.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - diensttypwunsch
  - shift type
  - preference
  - alias
  - free
  - early
  - late
  - night
  - wunsch
synonyms:
  de: [diensttypwunsch, schichtwunsch, alias, frei, früh, spät, nacht, möglichst arbeit]
  en: [shift type preference, preference, alias, free, early, late, night, wish]
  fr: [préférence de service, alias, libre, matin, après-midi, nuit, souhait]
  it: [preferenza turno, alias, libero, mattino, sera, notte, desiderio]
---

# Diensttypwünsche — den Assistenten lenken

## Kern-Idee (1 Satz)

**Diensttypwünsche** sind kurze Alias-Tokens, mit denen der Planer pro Tag und
Mitarbeiter eine **Wunsch-Schichtart** hinterlegt — der Planungs-Assistent
versucht den Wunsch zu erfüllen und greift auf **FREI** zurück, wenn er es
nicht kann.

## Die 8 Default-Aliase

| Alias | Bedeutung |
|---|---|
| `FREE` | **Frei** — bewusst kein Einsatz an diesem Tag. |
| `EARLY` | **Tagdienst** — nur Frühschichten werden vorgeschlagen. |
| `LATE` | **Abenddienst** — nur Spätschichten. |
| `NIGHT` | **Nachtdienst** — nur Nachtschichten. |
| `-FREE` | **Möglichst Arbeit** — Frei nur als allerletzte Wahl. |
| `-EARLY` | **Alles außer Tagdienst** — Spät/Nacht sind ok, kein Früh. |
| `-LATE` | **Alles außer Abenddienst** — Früh/Nacht sind ok, kein Spät. |
| `-NIGHT` | **Alles außer Nachtdienst** — Früh/Spät sind ok, keine Nacht. |

> Die Aliase sind **konfigurierbar**: Default-Namen können im Mandanten an
> die hauseigene Terminologie angepasst werden, die Semantik bleibt gleich.

## Verhalten des Assistenten

1. Erfüllbar → der gewünschte Diensttyp wird gewählt.
2. Nicht erfüllbar (kein passender Dienst frei) → Fallback auf `FREE`.
3. Negationen (`-…`) reduzieren den Lösungsraum, statt ihn zu fixieren.

## Wo werden die Wünsche eingegeben

Direkt in der Mitarbeiter-Zeile des Planungsblatts (Schedule-Section). Pro Tag
ein Alias. Mehrere Wünsche pro Tag sind nicht vorgesehen — pro Zelle exakt ein
Token.

## Verwandte Skills

- `explain_planning_assistant` — wie der Assistent die Wünsche verarbeitet
- `explain_planning_sheets_modular` — wo die Wünsche getragen werden

## Trigger-Phrasen

- "Was bedeutet `-EARLY` in der Planung?"
- "Wie sage ich dem Assistenten, dass jemand nur Nachtschichten will?"
- "Was ist `-FREE`?"
- "How do shift-type preferences work in Klacks?"
- "Comment exprimer un souhait de service ?"
