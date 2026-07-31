---
name: explain_klacksy_llm_setup
description: |
  Explains which language model Klacksy runs on and how providers and models are configured: adding
  a provider with its address and key, defining models with context size and cost, the default
  model, the nightly check for newly available models, and running Klacksy fully on a local model so
  no personal data leaves the building. Use this when the user asks which model is in use, how to
  connect another provider, why a model disappeared, or what a request costs.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - modell
  - model
  - provider
  - anbieter
  - openai
  - anthropic
  - api-schlüssel
  - api key
  - kosten
  - lokal
  - welches modell
synonyms:
  de: [modell, sprachmodell, anbieter, provider, api-schlüssel, schlüssel, kosten, token, lokales modell, welches modell nutzt du, standardmodell]
  en: [model, language model, provider, api key, cost, tokens, local model, which model do you use, default model]
  fr: [modèle, fournisseur, clé api, coût, modèle local, quel modèle]
  it: [modello, fornitore, chiave api, costo, modello locale, quale modello]
---

# The language model behind Klacksy

## Core idea (one sentence)

Klacksy is not tied to one vendor: the installation decides which providers and which concrete
models it may use — up to running entirely on a locally hosted model.

## Providers

A provider bundles one vendor's address, interface version and access key. Six vendors have a
dedicated implementation; every further provider is reached through a generic interface compatible
with the widely used standard, which covers hosted services and self-run models alike.

Klacksy can also suggest providers that are not set up yet — from a curated list of verified
addresses, or via web research if a web search is configured. Every suggestion is checked for
reachability before it is shown, and a chosen provider is created **disabled and without a key**, so
the key is added deliberately afterwards.

Once saved, an access key is never displayed again — the settings only show whether one is present.
A priority value decides in which order enabled providers are used.

## Models

Each model is defined under its provider with a display name, the vendor's technical identifier, its
context size, a token ceiling, and the cost per thousand input and output tokens. One model is
marked as the default. A provider cannot be removed while one of its models is the current default.

## The nightly check

A background service checks once a day which models the enabled providers have added or dropped. A
newly found model is first tried with a real request and only added, enabled, if that succeeds;
models no longer offered are disabled automatically. The full history stays readable per provider —
date, how many models were added, disabled or failed, and the test results. Administrators are
notified on their next sign-in if there are unread events. This runs in the background; nobody has
to keep a page open.

## Why local matters

Because the model is a free choice, Klacksy can run on a model inside the company's own network. In
that setup no personal record leaves the building — the deciding argument wherever staff data must
not go to an external service.

## Related skills

- `list_llm_providers` / `create_llm_provider` / `update_llm_provider` / `delete_llm_provider`
- `list_llm_models` / `create_llm_model` / `update_llm_model` / `delete_llm_model`

## Trigger phrases

- "Which model are you running on?"
- "Can I connect my own model?"
- "Why did that model disappear from the list?"
- "What does a request cost?"
- "Läuft das auch ohne Cloud?"
