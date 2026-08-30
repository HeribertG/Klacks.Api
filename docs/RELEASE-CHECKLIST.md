# Klacks.Api — Release Gate Checklist

Run this **before pushing a `vX.Y.Z` tag** (a `Klacks.Api` tag triggers the production deploy +
DB migrations — see the root `docs/knowledge/release-tagging-process-2026-06-04.md`). This
checklist gates the LLM/assistant surface that the deterministic CI spine (`tests.yml`) cannot
cover, because those tests need real provider credentials and are non-deterministic.

The commands below cost real money (real LLM provider calls) and take a while. They are
`[Explicit]` on purpose so they never run in the default suite or in CI — they run **only** when a
name filter selects them.

---

## 0. Deterministic CI must already be green

The three jobs in `Klacks.Api/.github/workflows/tests.yml` run on every push/PR to `main` and are
the baseline gate. Confirm the latest `main` run is green before tagging:

```bash
export PATH="$HOME/bin:$PATH"
gh run list --repo HeribertG/Klacks.Api --branch main --limit 5
```

Jobs (all must pass):

| Job | Project | DB | Covers |
|-----|---------|----|--------|
| `backend-tests`     | `Klacks.UnitTest`        | none          | unit / NSubstitute / InMemory — includes the goldset **schema** gates below |
| `api-tests`         | `Klacks.ApiTest`         | real Postgres | boots real `Program.cs` via `KlacksApiFactory` |
| `integration-tests` | `Klacks.IntegrationTest` | real Postgres | larger suite; **excludes** `TestCategory=Llm/ExternalApi/SlowModelLoad` |

The turn-eval goldset is schema-validated in CI (no DB, no LLM) by these unit tests — a broken
goldset fails `backend-tests`:

- `TurnGoldsetQualityTests` — version/kind, unique ids, every `expectedTool`/`alternativeTool`
  exists in `skill-seeds.json`, every `expectedSlot` is a real skill parameter, no-tool items
  have no slots.
- `FileTurnGoldsetLoaderTests`, `TurnEvalScorerTests`, `TurnEvalScorerRecipeExclusionTests`.

---

## 1. Two multi-LLM matrix E2E tests (explicit, DB-asserted)

Both fixtures are `[Explicit]` and assert on the **database effect**, not on chatbot prose. Start
the dev app first (they drive the real UI/DB on port 5434 — see `Klacks.E2ETest/E2E-DOCUMENTATION.md`
and the E2E rules). Each sweeps the 11-model matrix and takes ~10–15 min per model.

### 1a. Employee creation matrix

```bash
cd /mnt/c/SourceCode/Klacks.E2ETest
dotnet test --filter "FullyQualifiedName~ChatbotCreateEmployeeTest"
```

Asserts clean data per model: system-generated unique `id_number`, `type=Employee`, membership,
email, phone (prefix/value split), address with `country=CH` and non-empty state.

> This fixture is `[Explicit]` (it was previously `[Ignore]`, which a `--filter` **silently skips**
> while still exiting 0). If you ever see it report "0 tests ran", it has regressed to `[Ignore]` —
> re-check the attribute. `[Explicit]` is what makes the name filter actually run it.

### 1b. Cover-absence matrix

```bash
cd /mnt/c/SourceCode/Klacks.E2ETest
dotnet test --filter "FullyQualifiedName~ChatbotPlannerSkillsTest.Klacksy_CoversAbsence_PerModel"
```

Asserts the reactive disruption flow (record absence + propose rule-compliant cover as one isolated
scenario) lands correctly in the DB per model.

**Gate:** review the per-model pass/fail matrix printed by each run. A model that fails clean-data
assertions must not be shipped as a recommended/default model without a deliberate decision.

---

## 2. Review the latest nightly turn-eval scorecard

The nightly (`Klacks.Api/scripts/nightly-turn-eval.ps1`, local-only) replays the
`turn-selection-v1` goldset against the pinned model set (default: `deepseek-v4-pro`) and writes a
scorecard to `Klacks.Api/artifacts/turn-eval/turn-eval-<timestamp>.md`, reading composite + regression
back from the authoritative `eval_runs` table. Since 2026-08-30 a regression beyond the threshold
also makes the script exit with code 2 so the Windows Task Scheduler records a failed run.

- Open the most recent scorecard and confirm **no `>>> REGRESSION` line** and **no `WARNING`** for
  the prod-default model (`deepseek-v4-pro`).
- The scorecard now includes the item pass-rate gate from
  `Klacks.IntegrationTest/Assistant/TurnSelectionGoldenSetTests.cs` (min pass rate = latest baseline
  − 5 pp, overridable via `TURNEVAL_MIN_PASS_RATE`). A red gate in that test is a release blocker.
- If the newest scorecard is stale or missing, run it on demand before tagging:

```powershell
cd C:\SourceCode\Klacks.Api
powershell.exe -ExecutionPolicy Bypass -File scripts\nightly-turn-eval.ps1
```

Prerequisites: dev DB on `localhost:5434`; each evaluated model enabled in `llm_models` with its
provider enabled + keyed in `llm_providers` (keys live encrypted **in the DB**, never in the repo).
See the script header for the full contract and how to register it in Windows Task Scheduler.

---

## 3. Known coverage boundaries (do not mistake these for gaps to "fix" pre-release)

The turn-eval harness replays **single headless turns** and scores **tool selection + slot
arguments only**. It never executes a tool and never observes execution-time guard refusals. So:

- **Guard refusals are not measured by turn-eval.** The BulkAddBreaks duplicate-absence guard, the
  day-lock refusal, the non-tiling `cut_shift` rejection, and the missing-macro `create_shift`
  rejection all fire at execution time. Goldset items `ts-061` (add_break duplicate) and `ts-062`
  (place_work on a locked day) measure only that the model *reaches for* the write tool; the refusal
  itself is covered by backend unit/integration tests, not the goldset.
- **Recipe-forced writes are excluded from tool-accuracy** (flag `EngineRecipeWouldTrigger`). Absence
  placement (`ts-014`, `ts-061`) runs through an engine recipe in production, so the model's free
  tool choice is not scored — the item only keeps the recipe-forcing flag measured.
- **The confirmation turn ("ja, mach das" → `confirm_pending_action`) is intentionally NOT a goldset
  item.** Replay hardcodes an empty conversation history, so no pending confirmation token can exist,
  and in production the deterministic `AffirmationDetector` *replays* the held token — the LLM never
  selects `confirm_pending_action` from the toolset. Scoring LLM-selection here would measure a path
  that does not exist in production. Confirmation is covered by backend tests of the autonomy gate.

---

## 4. Tag

Only after 0–2 are satisfied, follow `docs/knowledge/release-tagging-process-2026-06-04.md`
(same `vX.Y.Z` tag on **both** `Klacks.Api` and `Klacks.Ui`; prefer `scripts/release.ps1`).
