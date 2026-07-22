# CI Spine (AP 0.3)

`tests.yml` in this repo and in `Klacks.Ui/.github/workflows/tests.yml` are the CI spine: they run
the deterministic test mass (Klacks.UnitTest, Klacks.ApiTest, Klacks.IntegrationTest,
Klacks.Ui unit tests + lint) on every push and pull request to `main`.

This document is written by an agent, not applied — no branch protection was changed as part of
this work package. Nothing here runs `gh` in write mode; run the commands yourself once you've
reviewed them.

## Job map

| Repo | Workflow | Job | What it needs | DB? |
|---|---|---|---|---|
| Klacks.Api | tests.yml | `backend-tests` | Klacks.UnitTest | no |
| Klacks.Api | tests.yml | `api-tests` | Klacks.ApiTest | yes (pgvector/pgvector:pg17 service) |
| Klacks.Api | tests.yml | `integration-tests` | Klacks.IntegrationTest | yes (pgvector/pgvector:pg17 service) |
| Klacks.Ui | tests.yml | `frontend-tests` | npm ci / vitest / eslint | no |

`Klacks.ApiTest` boots the real `Program.cs` via `WebApplicationFactory<Program>`
(`KlacksApiFactory`), which requires a live Postgres exactly like `Klacks.IntegrationTest` does —
despite the name, it is not a DB-free suite. Each of the two DB jobs gets its **own** fresh
`pgvector/pgvector:pg17` service container (not a shared one), so neither suite's seeded rows leak
into the other's assertions.

## Known trigger gap (read before assuming "every push is covered")

Klacks.Api, Klacks.UnitTest, Klacks.ApiTest and Klacks.IntegrationTest are four **separate**
GitHub repos (see the project's "CI Test-Repo separat" convention). A GitHub Actions workflow only
fires on events in the repo it lives in. `Klacks.Api/.github/workflows/tests.yml` therefore fires
on pushes to **Klacks.Api** and checks out the *current default-branch HEAD* of the three test
repos as siblings — it does **not** fire when someone pushes a test-only change directly to
Klacks.UnitTest/Klacks.ApiTest/Klacks.IntegrationTest with no matching Klacks.Api push.

This mirrors the existing, currently-green `deploy.yml` (same checkout pattern), so it is not a
regression — but it means: a test-only change pushed to a sibling test repo does not get CI
coverage until the next Klacks.Api push. Closing that gap would need either a `workflow_dispatch`
triggered from each test repo via `repository_dispatch` (requires a PAT with `repo` scope stored as
a secret in each test repo) or a small `tests.yml` duplicated into each test repo that checks out
Klacks.Api and its siblings the same way. Neither was built here — flagging it as an explicit,
open decision rather than silently leaving it uncovered.

## Cross-repo checkout: no new secret needed

The `token: ${{ secrets.GH_PAT || github.token }}` pattern in `tests.yml` mirrors `deploy.yml`
exactly. `GH_PAT` does **not** currently exist as a secret in Klacks.Api (`gh secret list --repo
HeribertG/Klacks.Api` confirms it), so today's checkouts fall back to the default `github.token`
for every sibling repo. All siblings involved (`Klacks.Plugin.Contracts`, `Klacks.Plugin.Messaging`,
`Klacks.Docs`, `Klacks.Api.SourceGenerators`, `Klacks.ScheduleOptimizer`, `Klacks.ScheduleRecovery`,
`Klacks.UnitTest`, `Klacks.ApiTest`, `Klacks.IntegrationTest`) are public — confirmed via
`git ls-remote` against each `https://github.com/HeribertG/<repo>.git` without credentials — and
`deploy.yml`'s last two runs on this exact checkout pattern were green
(`gh run list --repo HeribertG/Klacks.Api --workflow=deploy.yml`, 2026-07-22). **No secret needs to
be created for `tests.yml` to run.** If any of these repos is ever made private, add a `GH_PAT`
secret (a classic PAT with `repo` read scope) to Klacks.Api and the `|| github.token` fallback
picks it up automatically — no workflow change required.

## Branch protection (not applied — commands to run manually)

Requires an owner/admin token. Uses the GitHub CLI via the project's `~/bin/gh` PowerShell
wrapper (see root `CLAUDE.md`).

### Klacks.Api — require `backend-tests`, `api-tests`, `integration-tests`

```bash
cat <<'EOF' | gh api -X PUT repos/HeribertG/Klacks.Api/branches/main/protection --input -
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["backend-tests", "api-tests", "integration-tests"]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": null,
  "restrictions": null
}
EOF
```

### Klacks.Ui — require `frontend-tests`

```bash
cat <<'EOF' | gh api -X PUT repos/HeribertG/Klacks.Ui/branches/main/protection --input -
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["frontend-tests"]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": null,
  "restrictions": null
}
EOF
```

Verify afterwards:

```bash
gh api repos/HeribertG/Klacks.Api/branches/main/protection --jq '.required_status_checks.contexts'
gh api repos/HeribertG/Klacks.Ui/branches/main/protection --jq '.required_status_checks.contexts'
```

The job names above (`backend-tests`, `api-tests`, `integration-tests`, `frontend-tests`) are the
exact `jobs.<id>.name` values from `tests.yml` — GitHub matches required status checks by the
job's displayed name, so keep these two in sync if the workflow's job names ever change.

## Deploy gating

`deploy.yml`'s own `unit-tests` job duplicates a subset of what `tests.yml`'s `backend-tests` job
does (it is the pre-existing gate for the Hetzner deploy and was intentionally left untouched here
— out of scope for AP 0.3). Once branch protection above is in place, `main` cannot receive a
direct push/merge without `tests.yml`'s three Klacks.Api jobs passing first, so `deploy.yml`
running afterwards on the same push is already gated transitively through the protected branch —
no change to `deploy.yml` is needed for that ordering. If `deploy.yml` should also explicitly wait
for `tests.yml` (e.g. to avoid racing an in-flight `tests.yml` run on the same commit), add a
`workflow_run` trigger or a `needs:`-style gate; not done here since branch protection already
achieves the practical goal (nothing merges to `main` without green tests).

## What is NOT covered by this CI spine

- Klacks.E2ETest (Playwright) — not wired in; typically needs a running app + browser install and
  was out of scope for AP 0.3 ("deterministic test mass" only).
- LLM/golden-set tests (`TestCategory=Llm`, `TestCategory=ExternalApi`, `TestCategory=SlowModelLoad`
  in Klacks.IntegrationTest; `TestCategory=SlowModelLoad` in Klacks.UnitTest) — need real provider
  credentials/network, explicitly excluded by design (see task scope).
- Push-only-to-a-sibling-test-repo coverage gap — see "Known trigger gap" above.

## What remains unverified (no push was made; see report)

YAML syntax was validated locally, but no live GitHub Actions run has exercised this workflow (the
agent is prohibited from any git write operation). After the orchestrator pushes both `tests.yml`
files, verify via:

```bash
gh run list --repo HeribertG/Klacks.Api --workflow=tests.yml --limit 5
gh run list --repo HeribertG/Klacks.Ui --workflow=tests.yml --limit 5
gh run view <run-id> --repo HeribertG/Klacks.Api --log-failed   # if a job fails
```

Specifically unverified:
- That `psql` is actually preinstalled on the current `ubuntu-latest` image (documented as
  included by GitHub, not re-verified against the live image here).
- That `api-tests` and `integration-tests` each get a genuinely fresh, empty `klacks` database per
  run (expected, since each job is its own runner VM with its own service container — not observed
  in an actual run).
- Actual wall-clock time of `integration-tests` (~520 tests) inside the 30-minute job timeout.
- That `npm run lint` is currently clean on `main` (was not run as part of this work package).
