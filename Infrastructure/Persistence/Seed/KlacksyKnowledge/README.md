# KlacksyKnowledge — Konzept-Happen für die Klacksy-Skills

Dieser Ordner enthält **kleine, selbstständige Wissens-Happen** zu Kern-Konzepten
von Klacks. Jeder Happen ist eine eigene Markdown-Datei (1–2 KB), die

- eine 1-Satz-Definition,
- die Schlüssel-Datenfelder,
- ein konkretes Beispiel und
- mehrsprachige Trigger-Phrasen für den Embedding-Match

enthält.

## Wozu

Klacks indexiert registrierte Skills automatisch in der `knowledge_index`-Tabelle
(`Klacks.Api/Infrastructure/KnowledgeIndex/.../KnowledgeIndexSynchronizer.cs`).
Jeder Happen hier entspricht **einem zukünftigen "explain_X"-Skill**. Das YAML/JSON-Frontmatter
am Anfang jeder Datei ist bereits im Format von `skill-seeds.json` formuliert und
kann per Copy-Paste oder Skript in die Definitions-Datei eingespielt werden.

## Dateien

| Nr | Datei | Konzept |
|---|---|---|
| 01 | `planning-divide-et-impera.md` | Das Prinzip der modularen Planung |
| 02 | `planning-sheets-modular.md` | Planungsblätter mit Mitarbeiterzeilen |
| 03 | `planning-assistant.md` | Der Planungs-Assistent (Vorschläge, kein Zwang) |
| 04 | `macro-editor.md` | Macros für Dauer- und Lohnberechnung |
| 05 | `shift-type-preferences.md` | Diensttypwünsche FREE / EARLY / LATE / NIGHT |
| 06 | `shift-sporadic.md` | Sporadic Shift mit Scope + SumEmployees + Quantity |
| 07 | `shift-time-range.md` | TimeRange Shift mit elastischer Lage |
| 08 | `shift-container.md` | Container-Shift mit Template + Override |
| 09 | `shift-lifecycle-order-to-shift.md` | OriginalOrder → SealedOrder → OriginalShift → SplitShift |
| 10 | `address-management.md` | Client + zeitversionierte Adressen, Typen, Scope, Validierung |

## Ingest-Pfade

- **Variante A (DevKnowledge-Reingest)**:
  Die Dateien können über den DevKnowledge-Server eingespielt werden:
  `POST https://157.180.42.127:8443/api/knowledge/reingest` mit dem Inhalt
  einer Datei als Body. Die DevKnowledge-Skill `devknowledge-upload` liest aus
  diesem Ordner standardmäßig.

- **Variante B (Skill-Seed)**:
  Frontmatter eines Happens (`name`, `description`, `triggerKeywords`, `synonyms`)
  in `Klacks.Api/Application/Skills/Definitions/skill-seeds.json` als neuer
  Eintrag mergen. Beim nächsten Start synchronisiert `SkillSeedLoader` die
  Skills, und `KnowledgeIndexSynchronizer` indexiert das Description-Feld
  automatisch als Embedding-Quelle.

## Konvention für neue Happen

- **Max 2 KB pro Datei** — Klacksy-Antworten sollen knapp bleiben.
- **DE als Primärsprache**, mit Schlüsselbegriffen in EN/FR/IT für den Embedding-Match.
- **Frontmatter zuerst** mit den Skill-Metadaten (Name, Beschreibung, Trigger).
- **Dateiname = Skill-Name in kebab-case**, Skill-Name im Frontmatter in `snake_case`.
- **Quer-Verweis** auf andere Happen explizit über deren Skill-Name (z.B. `explain_container`).
