# Shift Workflow Documentation (Version 2.0)

## Übersicht

Dieses Dokument beschreibt den **korrekten** Workflow für Shift-Objekte im Klacks.Api System. Der wichtigste Punkt:

**Entweder OriginalShift (Status=2) ODER SplitShifts (Status=3) - NIEMALS beides gleichzeitig!**

**WICHTIG:** Der Unterschied zwischen **Seeding** (Datenbank-Setup) und **Runtime** (UI/Backend):
- **Runtime (Zerteilen in UI):** OriginalShift wird zu SplitShift geändert (UPDATE Status 2→3)
- **Seeding (Datenbank-Setup):** SplitShift ROOT wird DIREKT als Status=3 erstellt (KEIN UPDATE!)

## Shift Status (Enum)

```csharp
public enum ShiftStatus
{
    OriginalOrder = 0,    // Original-Auftrag/Bestellung
    SealedOrder = 1,      // Versiegelter Auftrag (GLEICHER Datensatz wie OriginalOrder!)
    OriginalShift = 2,    // Ursprüngliche Schicht (nicht geteilt)
    SplitShift = 3,       // Aufgeteilte Schicht (geteilt)
}
```

## Status-Regeln

### WICHTIGE REGEL:

Für eine Bestellung existiert **ENTWEDER**:
- **Ein** OriginalShift (Status=2) - Shift ist **nicht geteilt**

**ODER**

- **Mehrere** SplitShifts (Status=3) - Shift ist **geteilt**

**NIEMALS** existieren OriginalShift (Status=2) und SplitShift (Status=3) gleichzeitig!

## ⚠️ KRITISCH: Seeding vs. Runtime

### Runtime-Workflow (User zerteilt Shift in UI)

```
Schritt 1: OriginalShift existiert bereits (Status=2)
Schritt 2: User klickt "Zerteilen" → Backend updated zu Status=3
Schritt 3: Backend erstellt Children (Status=3)

→ UPDATE von Status 2 → 3 macht Sinn!
```

### Seeding-Workflow (Datenbank-Setup)

Beim Seeding wissen wir **schon vorher**, ob ein Shift zerteilt werden soll!

**ENTWEDER (nicht geteilt):**
```sql
INSERT INTO shift (status=2, lft=NULL, rgt=NULL, ...); -- OriginalShift
```

**ODER (geteilt):**
```sql
INSERT INTO shift (status=3, lft=1, rgt=8, ...);  -- SplitShift ROOT (DIREKT Status=3!)
INSERT INTO shift (status=3, parent_id=..., ...); -- Child 1
INSERT INTO shift (status=3, parent_id=..., ...); -- Child 2
```

**❌ NIEMALS beim Seeding:**
```sql
INSERT INTO shift (status=2, ...);           -- OriginalShift erstellen
UPDATE shift SET status=3, lft=1, rgt=8;     -- Zu SplitShift updaten ❌ FALSCH!
```

**Regel:** Beim Seeding gibt es KEIN UPDATE von Status 2→3. Der ROOT wird direkt als Status=3 erstellt!

## Workflow-Phasen

### Phase 1: Order-Erstellung und Versiegelung (EIN Datensatz)

```
┌─────────────────────────────┐
│ OriginalOrder (Status = 0)  │
│ ───────────────────────────  │
│ ID: xxx                      │  ← EINE ID
│ parent_id: NULL              │
│ root_id: NULL                │
│ original_id: NULL            │
│ lft: NULL                    │
│ rgt: NULL                    │
└──────────────┬──────────────┘
               │
               │ UPDATE shift SET status = 1 WHERE id = 'xxx'
               │ (KEIN neuer Datensatz!)
               ▼
┌─────────────────────────────┐
│ SealedOrder (Status = 1)     │
│ ───────────────────────────  │
│ ID: xxx                      │  ← GLEICHE ID!
│ parent_id: NULL              │
│ root_id: NULL                │
│ original_id: NULL            │  ← Bleibt NULL!
│ lft: NULL                    │
│ rgt: NULL                    │
└──────────────┬──────────────┘
```

**WICHTIG:**
- OriginalOrder und SealedOrder sind **DERSELBE Datensatz**
- Nur der `status` ändert sich von 0 auf 1
- `original_id` bleibt **NULL**

### Phase 2a: OriginalShift-Erstellung (Nicht geteilt)

```
┌─────────────────────────────┐
│ SealedOrder (Status = 1)     │
│ ID: xxx                      │
└──────────────┬──────────────┘
               │
               │ INSERT neuer Shift
               ▼
┌─────────────────────────────┐
│ OriginalShift (Status = 2)   │
│ ───────────────────────────  │
│ ID: yyy                      │  ← NEUE ID!
│ parent_id: NULL              │
│ root_id: NULL                │
│ original_id: xxx             │  ← Zeigt auf SealedOrder!
│ lft: NULL                    │  ← Kein Nested Set (nicht geteilt)
│ rgt: NULL                    │
└─────────────────────────────┘
```

**WICHTIG:**
- OriginalShift ist ein **NEUER Datensatz** (neue ID)
- `parent_id` = **NULL**
- `root_id` = **NULL**
- `original_id` zeigt auf den **SealedOrder** (ID xxx)
- `lft` und `rgt` sind **NULL** (kein Nested Set, da nicht geteilt)

**STOP HIER** - wenn der Shift **nicht geteilt** wird, bleibt er als OriginalShift (Status=2)!

### Phase 2b: Shift-Aufteilung (OriginalShift → SplitShifts) - RUNTIME ONLY!

**⚠️ WICHTIG: Dieser Workflow gilt NUR für RUNTIME (User zerteilt in UI), NICHT für Seeding!**

Wenn der OriginalShift **zur Laufzeit geteilt** werden soll:

```
┌─────────────────────────────┐
│ OriginalShift (Status = 2)   │
│ ID: yyy                      │
│ lft: NULL, rgt: NULL         │
└──────────────┬──────────────┘
               │
               │ UPDATE shift SET status = 3, lft = 1, rgt = 2 WHERE id = 'yyy'
               │ (OriginalShift wird zu SplitShift!)
               ▼
┌─────────────────────────────┐
│ SplitShift (Status = 3)      │  ← Der ursprüngliche OriginalShift!
│ ───────────────────────────  │
│ ID: yyy                      │  ← GLEICHE ID!
│ parent_id: NULL              │  ← Bleibt NULL (ist Root)
│ root_id: NULL                │  ← Bleibt NULL (ist Root)
│ original_id: xxx             │  ← Bleibt gleich
│ lft: 1                       │  ← Nested Set Start (ist jetzt Root des Trees)
│ rgt: 8                       │  ← Nested Set Ende (Beispiel für 3 Kinder)
└──────────────┬──────────────┘
               │
       ────────┼────────┬─────────
       │               │         │
       ▼               ▼         ▼
┌────────────┐  ┌────────────┐  ┌────────────┐
│ SplitShift │  │ SplitShift │  │ SplitShift │
│ Status = 3 │  │ Status = 3 │  │ Status = 3 │
│ ──────────│  │ ──────────│  │ ──────────│
│ ID: aaa    │  │ ID: bbb    │  │ ID: ccc    │
│ parent: yyy│  │ parent: yyy│  │ parent: yyy│
│ root: yyy  │  │ root: yyy  │  │ root: yyy  │
│ orig: xxx  │  │ orig: xxx  │  │ orig: xxx  │
│ lft: 2     │  │ lft: 4     │  │ lft: 6     │
│ rgt: 3     │  │ rgt: 5     │  │ rgt: 7     │
│ 07:00-15:00│  │ 15:00-23:00│  │ 23:00-07:00│
└────────────┘  └────────────┘  └────────────┘
```

**WICHTIG:**
- Der **ursprüngliche OriginalShift** (ID yyy) wird zu einem **SplitShift** (Status 2 → 3)
- Der SplitShift yyy wird zum **Root** des Nested Set Trees (parent_id=NULL, root_id=NULL, lft=1, rgt=8)
- **Neue SplitShifts** (aaa, bbb, ccc) werden als **Children** erstellt
- Alle SplitShifts zeigen mit `parent_id` und `root_id` auf den ursprünglichen Shift (yyy)
- Alle zeigen mit `original_id` auf den SealedOrder (xxx)

## Vollständiger Workflow-Überblick

### Variante A: Nicht geteilter Shift

```
Datensatz 1: Order
┌─────────────────┐
│ ID: xxx         │  INSERT mit Status = 0 (OriginalOrder)
│ Status: 0       │       │
│ parent: NULL    │       │ UPDATE status = 1
│ root: NULL      │       ▼
│ original: NULL  │  Status = 1 (SealedOrder)
│ lft/rgt: NULL   │
└─────────────────┘
         │
         │ Neuer Datensatz wird erstellt
         ▼
Datensatz 2: OriginalShift
┌─────────────────┐
│ ID: yyy         │  INSERT mit Status = 2
│ Status: 2       │
│ parent: NULL    │
│ root: NULL      │
│ original: xxx   │  ← Zeigt auf SealedOrder
│ lft: NULL       │  ← Kein Nested Set (nicht geteilt)
│ rgt: NULL       │
└─────────────────┘

** FERTIG ** - Shift ist nicht geteilt
```

### Variante B (Runtime): Geteilter Shift - User zerteilt in UI

**⚠️ NUR für Runtime, wenn User einen bestehenden OriginalShift zerteilt!**

```
Datensatz 1: Order
┌─────────────────┐
│ ID: xxx         │  INSERT mit Status = 0 (OriginalOrder)
│ Status: 0       │       │
│ parent: NULL    │       │ UPDATE status = 1
│ root: NULL      │       ▼
│ original: NULL  │  Status = 1 (SealedOrder)
│ lft/rgt: NULL   │
└─────────────────┘
         │
         │ Backend erstellt automatisch
         ▼
Datensatz 2: OriginalShift (existiert bereits)
┌─────────────────┐
│ ID: yyy         │  INSERT mit Status = 2
│ Status: 2       │       │
│ parent: NULL    │       │ User klickt "Zerteilen"
│ root: NULL      │       │ → UPDATE status = 3, lft = 1, rgt = 8
│ original: xxx   │       ▼
│ lft: NULL → 1   │  Status = 3 (SplitShift - Root)
│ rgt: NULL → 8   │
└─────────────────┘
         │
         │ Backend erstellt neue Children
         ▼
Datensätze 3-5: SplitShifts (Children)
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ ID: aaa      │  │ ID: bbb      │  │ ID: ccc      │
│ Status: 3    │  │ Status: 3    │  │ Status: 3    │
│ parent: yyy  │  │ parent: yyy  │  │ parent: yyy  │
│ root: yyy    │  │ root: yyy    │  │ root: yyy    │
│ original: xxx│  │ original: xxx│  │ original: xxx│
│ lft: 2, 3    │  │ lft: 4, 5    │  │ lft: 6, 7    │
└──────────────┘  └──────────────┘  └──────────────┘
```

### Variante B (Seeding): Geteilter Shift - Datenbank-Setup

**⚠️ NUR für Seeding, wenn Daten initial geladen werden!**

```
Datensatz 1: Order
┌─────────────────┐
│ ID: xxx         │  INSERT mit Status = 0 (OriginalOrder)
│ Status: 0       │       │
│ parent: NULL    │       │ UPDATE status = 1
│ root: NULL      │       ▼
│ original: NULL  │  Status = 1 (SealedOrder)
│ lft/rgt: NULL   │
└─────────────────┘
         │
         │ Seeding erstellt DIREKT SplitShift ROOT
         ▼
Datensatz 2: SplitShift ROOT (DIREKT Status=3, KEIN Status=2!)
┌─────────────────┐
│ ID: yyy         │  INSERT mit Status = 3 (DIREKT!)
│ Status: 3       │  (KEIN UPDATE von 2→3!)
│ parent: NULL    │
│ root: NULL      │
│ original: xxx   │
│ lft: 1          │  ← lft/rgt SOFORT gesetzt
│ rgt: 8          │
└─────────────────┘
         │
         │ Seeding erstellt Children
         ▼
Datensätze 3-5: SplitShifts (Children)
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ ID: aaa      │  │ ID: bbb      │  │ ID: ccc      │
│ Status: 3    │  │ Status: 3    │  │ Status: 3    │
│ parent: yyy  │  │ parent: yyy  │  │ parent: yyy  │
│ root: yyy    │  │ root: yyy    │  │ root: yyy    │
│ original: xxx│  │ original: xxx│  │ original: xxx│
│ lft: 2, 3    │  │ lft: 4, 5    │  │ lft: 6, 7    │
└──────────────┘  └──────────────┘  └──────────────┘
```

## Beziehungen (Relations)

### 1. `original_id` Beziehung

- **Order (Status 0/1):** `original_id = NULL`
- **OriginalShift (Status 2):** `original_id = SealedOrder.Id`
- **SplitShift (Status 3):** `original_id = SealedOrder.Id`

**Regel:** Alle Shifts (Status ≥ 2) zeigen mit `original_id` auf ihren SealedOrder.

### 2. Nested Set Tree (parent_id, root_id, lft, rgt)

- **Order (Status 0/1):** Kein Nested Set (alle NULL)
- **OriginalShift (Status 2):** Kein Nested Set (alle NULL) - nicht geteilt!
- **SplitShift Root (Status 3):** Basis des Nested Set Trees
  - `parent_id = NULL`
  - `root_id = NULL`
  - `lft = 1`
  - `rgt` umschließt alle Kinder (z.B. 8 für 3 Kinder)
- **SplitShift Children (Status 3):** Kinder im Tree
  - `parent_id = Root SplitShift.Id`
  - `root_id = Root SplitShift.Id`
  - `lft` und `rgt` innerhalb des Root-Bereichs

### 3. Group-Beziehung (GroupItem)

**KRITISCH:** Alle Shifts im gleichen Workflow MÜSSEN die GLEICHEN Groups haben!

```
Order (xxx) → Groups: [ZH, BE]
    │
    ├─ OriginalShift (yyy) → Groups: [ZH, BE]  ← GLEICHE!
    │  (wird zu SplitShift)
    │
    ├─ SplitShift Root (yyy) → Groups: [ZH, BE]  ← GLEICHE!
    │   │
    │   ├─ SplitShift Child 1 (aaa) → Groups: [ZH, BE]  ← GLEICHE!
    │   ├─ SplitShift Child 2 (bbb) → Groups: [ZH, BE]  ← GLEICHE!
    │   └─ SplitShift Child 3 (ccc) → Groups: [ZH, BE]  ← GLEICHE!
```

## Datenbank-Operationen

### Korrekte Seed-Reihenfolge

#### Nicht geteilter Shift:

```sql
-- 1. Order erstellen (Status = 0)
INSERT INTO shift (id, status, parent_id, root_id, original_id, lft, rgt, ...)
VALUES ('xxx', 0, NULL, NULL, NULL, NULL, NULL, ...);

-- 2. Groups für Order zuweisen
INSERT INTO group_item (id, shift_id, group_id, ...)
VALUES ('g1', 'xxx', 'group_zh', ...);

-- 3. Order versiegeln (Status 0 → 1)
UPDATE shift
SET status = 1, update_time = NOW()
WHERE id = 'xxx';

-- 4. OriginalShift erstellen (Status = 2)
INSERT INTO shift (id, status, parent_id, root_id, original_id, lft, rgt, ...)
VALUES ('yyy', 2, NULL, NULL, 'xxx', NULL, NULL, ...);

-- 5. Groups für OriginalShift zuweisen (GLEICHE wie Order!)
INSERT INTO group_item (id, shift_id, group_id, ...)
VALUES ('g3', 'yyy', 'group_zh', ...);

-- ** FERTIG ** - Shift ist nicht geteilt
```

#### Geteilter Shift:

```sql
-- 1. Order erstellen (Status = 0)
INSERT INTO shift (id, status, parent_id, root_id, original_id, lft, rgt, ...)
VALUES ('xxx', 0, NULL, NULL, NULL, NULL, NULL, ...);

-- 2. Groups für Order zuweisen
INSERT INTO group_item (id, shift_id, group_id, ...)
VALUES ('g1', 'xxx', 'group_zh', ...);

-- 3. Order versiegeln (Status 0 → 1)
UPDATE shift
SET status = 1, update_time = NOW()
WHERE id = 'xxx';

-- 4. SplitShift ROOT erstellen (Status = 3 DIREKT, KEIN Status 2!)
INSERT INTO shift (id, status, parent_id, root_id, original_id, lft, rgt, ...)
VALUES ('yyy', 3, NULL, NULL, 'xxx', 1, 8, ...);
--            ↑ DIREKT Status=3, nicht Status=2!
--                                              ↑ lft/rgt sofort gesetzt!

-- 5. Groups für SplitShift ROOT zuweisen (GLEICHE wie Order!)
INSERT INTO group_item (id, shift_id, group_id, ...)
VALUES ('g3', 'yyy', 'group_zh', ...);

-- 6. SplitShift Child 1 erstellen (Status = 3)
INSERT INTO shift (id, status, parent_id, root_id, original_id, lft, rgt, ...)
VALUES ('aaa', 3, 'yyy', 'yyy', 'xxx', 2, 3, ...);

-- 7. Groups für SplitShift Child 1 zuweisen (GLEICHE!)
INSERT INTO group_item (id, shift_id, group_id, ...)
VALUES ('g5', 'aaa', 'group_zh', ...);

-- 8. SplitShift Child 2 erstellen (Status = 3)
INSERT INTO shift (id, status, parent_id, root_id, original_id, lft, rgt, ...)
VALUES ('bbb', 3, 'yyy', 'yyy', 'xxx', 4, 5, ...);

-- 9. Groups für SplitShift Child 2 zuweisen (GLEICHE!)
INSERT INTO group_item (id, shift_id, group_id, ...)
VALUES ('g6', 'bbb', 'group_zh', ...);

-- 10. SplitShift Child 3 erstellen (Status = 3)
INSERT INTO shift (id, status, parent_id, root_id, original_id, lft, rgt, ...)
VALUES ('ccc', 3, 'yyy', 'yyy', 'xxx', 6, 7, ...);

-- 11. Groups für SplitShift Child 3 zuweisen (GLEICHE!)
INSERT INTO group_item (id, shift_id, group_id, ...)
VALUES ('g7', 'ccc', 'group_zh', ...);
```

## Häufige Fehler

### ❌ FALSCH: OriginalShift und SplitShifts existieren gleichzeitig

```sql
-- OriginalShift erstellen (Status = 2)
INSERT INTO shift VALUES ('yyy', 2, NULL, NULL, 'xxx', NULL, NULL, ...);

-- SplitShift erstellen OHNE OriginalShift zu ändern (FALSCH!)
INSERT INTO shift VALUES ('aaa', 3, 'yyy', 'yyy', 'xxx', 2, 3, ...);
```

**Problem:** Jetzt existiert ein OriginalShift (Status=2) UND SplitShifts (Status=3) gleichzeitig!

### ✅ RICHTIG für RUNTIME: OriginalShift wird zu SplitShift geändert

**NUR für Runtime (User zerteilt in UI):**

```sql
-- OriginalShift existiert bereits (Status = 2)
-- User klickt "Zerteilen" → Backend updated:
UPDATE shift SET status = 3, lft = 1, rgt = 8 WHERE id = 'yyy';

-- Weitere SplitShift Children erstellen
INSERT INTO shift VALUES ('aaa', 3, 'yyy', 'yyy', 'xxx', 2, 3, ...);
```

### ✅ RICHTIG für SEEDING: SplitShift ROOT direkt erstellen

**NUR für Seeding (Datenbank-Setup):**

```sql
-- SplitShift ROOT DIREKT als Status=3 erstellen (KEIN UPDATE!)
INSERT INTO shift VALUES ('yyy', 3, NULL, NULL, 'xxx', 1, 8, ...);

-- SplitShift Children erstellen
INSERT INTO shift VALUES ('aaa', 3, 'yyy', 'yyy', 'xxx', 2, 3, ...);
INSERT INTO shift VALUES ('bbb', 3, 'yyy', 'yyy', 'xxx', 4, 5, ...);
INSERT INTO shift VALUES ('ccc', 3, 'yyy', 'yyy', 'xxx', 6, 7, ...);
```

### ❌ FALSCH: OriginalShift hat lft/rgt Werte

```sql
-- OriginalShift mit Nested Set erstellen (FALSCH!)
INSERT INTO shift (id, status, lft, rgt, ...)
VALUES ('yyy', 2, 1, 8, ...);
```

**Problem:** OriginalShift (Status=2) sollte **KEINE** lft/rgt Werte haben, da er noch nicht geteilt ist!

### ✅ RICHTIG: OriginalShift ohne lft/rgt

```sql
-- OriginalShift OHNE Nested Set erstellen (RICHTIG!)
INSERT INTO shift (id, status, lft, rgt, ...)
VALUES ('yyy', 2, NULL, NULL, ...);

-- Erst beim UPDATE zu SplitShift kommen lft/rgt dazu
UPDATE shift SET status = 3, lft = 1, rgt = 8 WHERE id = 'yyy';
```

## Zusammenfassung

1. **OriginalOrder → SealedOrder**: GLEICHER Datensatz, nur Status-Update (0 → 1)
2. **SealedOrder → OriginalShift**: NEUER Datensatz mit `lft = NULL`, `rgt = NULL`
3. **ENTWEDER:**
   - **OriginalShift (Status=2)** bleibt bestehen - Shift ist **nicht geteilt**
4. **ODER (Runtime):**
   - **OriginalShift → SplitShift**: UPDATE Status 2 → 3, lft/rgt werden gesetzt
   - **Weitere SplitShifts**: NEUE Children-Datensätze im Nested Set
5. **ODER (Seeding):**
   - **SplitShift ROOT**: DIREKT als Status=3 erstellen (KEIN Status=2, KEIN UPDATE!)
   - **SplitShift Children**: NEUE Children-Datensätze im Nested Set
6. **Groups**: IMMER die gleichen im gesamten Workflow
7. **original_id**: Zeigt bei allen Shifts (Status ≥ 2) auf den SealedOrder
8. **Nested Set**: Nur SplitShifts (Status = 3) verwenden `lft` und `rgt`

**WICHTIG:**
- Für eine Bestellung existiert **NIEMALS** gleichzeitig ein OriginalShift (Status=2) UND SplitShifts (Status=3)!
- **Seeding:** SplitShift ROOT wird DIREKT als Status=3 erstellt (kein UPDATE von 2→3)
- **Runtime:** OriginalShift wird per UPDATE zu SplitShift (2→3)

---

**Erstellt:** 2025-10-26
**Letzte Aktualisierung:** 2025-10-26
**Version:** 2.1 - Korrigiert: Seeding vs. Runtime
**Autor:** Klacks Development Team
