---
name: explain_planning_divide_et_impera
description: |
  Explains Klacks' modular planning philosophy "Divide et impera" (teile und herrsche).
  Use this when the user asks about modular planning, how to break large plans into smaller
  ones, why Klacks splits work across multiple Planungsblätter (planning sheets), or how
  Mitarbeiterausleihe (employee borrowing between business units) avoids overlap conflicts.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - planning
  - planung
  - modular
  - divide
  - herrsche
  - teilen
  - zerlegen
  - planungsblatt
synonyms:
  de: [planung, modulplanung, teilplanung, zerlegen, aufteilen, geschäftsfeld, ausleihe]
  en: [planning, modular, divide, conquer, split, sub-plan, lend, borrow]
  fr: [planification, modulaire, diviser, sous-plan, prêter]
  it: [pianificazione, modulare, dividere, sotto-piano, prestare]
---

# Divide et impera — Klacks' Planungs-Philosophie

## Kern-Idee (1 Satz)

Klacks zerlegt jede Planung in kleinere, abgeschlossene **Planungsblätter** und arbeitet
diese nacheinander ab — übergreifende Änderungen propagieren automatisch zwischen den
Blättern, damit Überschneidungen ausgeschlossen sind.

## Warum modular planen

- **Komplexität bändigen**: Eine große Gesamtplanung wird zerteilt in funktionale
  Einheiten, die jede für sich übersichtlich bleibt.
- **Mitarbeiter-Ausleihe**: Verschiedene Geschäftsfelder leihen sich gegenseitig
  Mitarbeiter — die Datenstruktur stellt sicher, dass keine Fehlplanungen wegen
  Überschneidungen entstehen.
- **Konsistenz**: Wird Mitarbeiter Y im Planungsblatt 1 geändert, ist die Änderung
  auch im Planungsblatt 2 sichtbar — dieselbe Person, geteilte Sicht.

## Praktische Konsequenz

Ein Planer sieht nur den Ausschnitt, den er gerade braucht, und arbeitet wie an einer
abgeschlossenen Mini-Planung. Trotzdem ist die Datenbasis konzernweit synchron.

## Verwandte Skills

- `explain_planning_sheets_modular` — die Blätter selbst
- `explain_planning_assistant` — automatische Vorschläge

## Trigger-Phrasen

- "Wie funktioniert modulare Planung in Klacks?"
- "Warum sehe ich denselben Mitarbeiter in zwei Planungsblättern?"
- "How does Klacks split planning into smaller units?"
- "Was heißt Divide et impera in Klacks?"
