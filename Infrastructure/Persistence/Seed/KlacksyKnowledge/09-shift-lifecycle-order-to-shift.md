---
name: explain_shift_lifecycle_order_to_shift
description: |
  Explains the four-stage lifecycle of a Shift in Klacks — from editable
  OriginalOrder (draft) via immutable SealedOrder (contract) to active
  OriginalShift and its SplitShift cuts. The Sealing transition is the only
  trigger that clones the OriginalShift from the SealedOrder. Works can only
  attach to OriginalShift or SplitShift (Status >= 2). Use this when the user
  asks why a draft is not bookable, why a sealed order is immutable, or what
  ShiftStatus values mean.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - lifecycle
  - lebenszyklus
  - originalorder
  - sealedorder
  - originalshift
  - splitshift
  - bestellung
  - sealing
  - sealen
  - workflow
synonyms:
  de: [bestellung, auftrag, skizze, vertrag, sealen, versiegeln, schicht-schnitt, lebenszyklus]
  en: [order, draft, contract, sealed, lifecycle, shift cut, status]
  fr: [commande, ébauche, contrat, verrouillé, cycle de vie, coupe]
  it: [ordine, bozza, contratto, sigillato, ciclo di vita, taglio]
---

# Workflow Bestellung → Shift — die 4 Status-Stufen

## Kern-Idee (1 Satz)

Ein Shift durchläuft vier `ShiftStatus`-Werte — `OriginalOrder` (Skizze),
`SealedOrder` (immutable Vertrag), `OriginalShift` (aktive Planung) und
`SplitShift` (Tages-/Zeitschnitt) — und **genau eine** Transition, das Sealen,
erzeugt **einmalig** den aktiven Planungs-Datensatz.

## Die vier Status-Werte

| Wert | Status | Bedeutung |
|---|---|---|
| 0 | `OriginalOrder` | **Skizze / Bestellung**. Frei editierbar. Nicht im Einsatzplan. Nicht buchbar. |
| 1 | `SealedOrder` | **Vertrag / freigegebener Auftrag**. Permanent immutable. Auftrags-Snapshot. |
| 2 | `OriginalShift` | **Aktiver Planungs-Datensatz**. Geklont aus SealedOrder. Trägt `OriginalId` → SealedOrder. Editierbar, schneidbar, buchbar. |
| 3 | `SplitShift` | **Tages-/Zeitschnitt** des OriginalShift. Trägt `ParentId` und `RootId` (Nested-Set Lft/Rgt). |

## Die einmalige Sealing-Transition

```
┌──────────────────┐  PUT Status=SealedOrder  ┌──────────────────┐
│  OriginalOrder   │ ───────────────────────► │   SealedOrder    │
│  (Status = 0)    │   (einmaliger Trigger,   │  (Status = 1)    │
│  Skizze          │    klont SIMULTAN        │  unveränderlich  │
└──────────────────┘    OriginalShift)        └──────────────────┘
                                                        │
                                                        │ Klon nur HIER
                                                        ▼
┌──────────────────┐    ShiftCutFacade        ┌──────────────────┐
│   SplitShift     │ ◄─────────────────────── │  OriginalShift   │
│  (Status = 3)    │    (2-Phasen-Cut)        │  (Status = 2)    │
└──────────────────┘                          └──────────────────┘
```

`ShiftRepository.PutWithSealedOrderHandling` cloned **nur**, wenn:

```
updatedShift.Status == SealedOrder  AND  previousStatus != SealedOrder
```

Bei jedem späteren PUT auf einem bereits gesealten Shift → kein erneuter Klon.

## Wo Mitarbeiter gebucht werden

`Work`-Entitäten hängen **ausschließlich** an Shifts mit Status ≥ 2 — also
`OriginalShift` oder `SplitShift`. Versuche, auf `OriginalOrder` oder
`SealedOrder` zu buchen, sind technisch und semantisch ausgeschlossen.

## Beispiel-Lebenszyklus

```
2026-05-01  Anwender: neuer Shift "Hochzeit Müller" (OriginalOrder).
            Iterative Verfeinerung der Details.

2026-05-03  Kunde bestätigt → Status=SealedOrder + Save.
            Backend: SealedOrder festgeschrieben (id=A) + Klon OriginalShift
            (id=B, OriginalId=A) erzeugt.

2026-05-04  Disposition öffnet id=B, trägt 2 Mitarbeiter ein.

2026-05-05  Anwender zerschneidet id=B in 2 SplitShifts (id=C, id=D),
            ParentId=B, RootId=B.

2026-06-01  Periode geschlossen → LockLevel=Closed auf den Works auf C+D.
            id=A (SealedOrder) ist nie verändert worden — bleibt als Vertrag.
```

## Orthogonales Locking

Einzelne `Work`/`Break`-Einträge tragen ein separates `LockLevel` (None /
Confirmed / Approved / Closed). Das ist **NICHT** zu verwechseln mit
`ShiftStatus.SealedOrder`. Sealing eines Works ist Entry-Level, Sealing
eines Shifts ist Lifecycle-Level.

## Verwandte Skills

- `explain_shift_sporadic` / `explain_shift_time_range` / `explain_shift_container`
- `explain_planning_assistant` — der Assistent bucht an OriginalShift/SplitShift

## Trigger-Phrasen

- "Was ist der Unterschied zwischen OriginalOrder und OriginalShift?"
- "Wann wird ein Auftrag zu einer Schicht?"
- "Warum kann ich meine Bestellung nach dem Speichern nicht mehr ändern?"
- "Wie funktioniert das Sealing eines Auftrags?"
- "How does Klacks transition from order to shift?"
- "Pourquoi ma commande est-elle verrouillée ?"
- "Cosa significa SealedOrder?"
