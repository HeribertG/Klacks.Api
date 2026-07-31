---
name: explain_navigation_learning
description: |
  Explains how Klacksy finds the right page from a spoken or typed request, why it sometimes opens a
  list and sometimes a single record, and what happens when it cannot map a phrasing at all: the
  attempt is recorded as unresolved feedback, an administrator sees the collected wordings and
  teaches them as additional phrasings for that page. Use this when Klacksy opens the wrong page,
  does not understand a way of asking, or somebody wants to teach it the words used in-house.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - findet die seite nicht
  - falsche seite
  - navigations-synonym
  - versteht mich nicht
  - beibringen
synonyms:
  de: [findet die seite nicht, öffnet die falsche seite, versteht meine formulierung nicht, navigations-synonym, klacksy etwas beibringen, unser wort dafür, trainingsseite]
  en: [does not find the page, opens the wrong page, does not understand my wording, navigation synonym, teach klacksy, our word for it, training page]
  fr: [ne trouve pas la page, ouvre la mauvaise page, ne comprend pas ma formulation, synonyme de navigation, apprendre à klacksy]
  it: [non trova la pagina, apre la pagina sbagliata, non capisce la mia formulazione, sinonimo di navigazione, insegnare a klacksy]
---

# How Klacksy finds a page — and how it learns new wordings

## Core idea (one sentence)

Every page has a set of phrasings people use for it, and when somebody uses one Klacksy does not
know, that attempt is recorded so an administrator can teach it.

## Three ways of getting somewhere

Depending on what is asked, Klacksy does one of three things — which explains why the result is
sometimes a list and sometimes a single record:

- **Straight to a page** — "open the schedule". The page opens as it is.
- **A list, optionally narrowed** — "show me all customers in Bern". A list page opens, already
  filtered.
- **Straight to one record** — "open Max Müller". Klacksy searches first and navigates to the hit.
  If several match, it asks which one rather than guessing.

## When a wording is not recognised

Phrasings differ between companies. What one calls the duty roster another calls the deployment
plan, and a third has an in-house term nobody outside would guess.

If Klacksy cannot confidently map a request to a page, it **does not open a random one**. The
attempt is recorded as unresolved feedback, together with what it did match. Recurring wordings
therefore accumulate visibly instead of being lost in individual conversations.

## Teaching it

On the training page an administrator sees two things: the collected unresolved wordings, and all
navigation targets with their current phrasings and a review status per language. A wording that
keeps coming up is added to the matching target — **added**, not replaced, so existing phrasings
stay intact and duplicates are ignored.

Phrasings are held **per language**, so a term used in the German-speaking office does not have to
work in the French one and vice versa.

This makes the loop concrete: people ask in their own words → unrecognised wordings are collected →
an administrator assigns them once → everybody's phrasing works from then on.

## Related skills

- `navigate_to` — open a page directly
- `search_in_list` — open a list page, optionally filtered
- `search_and_navigate` — find a specific record and open it
- `list_navigation_feedback` — the collected unrecognised wordings
- `list_navigation_targets` / `update_navigation_synonyms` — the pages and their phrasings

## Trigger phrases

- "Why did you open the wrong page?"
- "You don't understand what I call it."
- "We say 'deployment plan' here, not 'schedule'."
- "How do I teach you our own terms?"
- "Warum findest du die Seite nicht?"
