---
name: explain_klacksy_memory
description: |
  Explains what Klacksy remembers between conversations and the difference between company-wide
  memory (visible to everyone, admin-managed) and personal memory (only about the signed-in user).
  Also covers reminder notes that Klacksy holds back and delivers later. Use this when the user asks
  what Klacksy knows about them, why it still remembers something, how to make it remember or forget
  a fact, or where a note they dictated earlier went.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - remember
  - merken
  - merkst
  - gedächtnis
  - memory
  - vergiss
  - forget
  - notiz
  - erinnerung
  - reminder
synonyms:
  de: [gedächtnis, merken, erinnern, vergessen, gemerkt, notiz, erinnerung, was weisst du über mich, persönliche angabe]
  en: [memory, remember, forget, recall, personal fact, note, reminder, what do you know about me]
  fr: [mémoire, se souvenir, oublier, note, rappel, que sais-tu de moi]
  it: [memoria, ricordare, dimenticare, nota, promemoria, cosa sai di me]
---

# Klacksy's memory — what it keeps between conversations

## Core idea (one sentence)

Klacksy keeps facts beyond the end of a conversation, and every fact belongs either to the whole
company or to one single person — that distinction decides who gets to see it and who may change it.

## The two kinds of memory

**Company memory** holds facts that apply to everyone: company rules, recurring arrangements,
domain knowledge. Only administrators can add, change or delete these. Durable company-wide facts
are pinned automatically and carry high importance, so they stay available in every single turn
rather than being fetched only when they look relevant.

**Personal memory** holds facts about the signed-in user — preferences, how they like to be
addressed, which teams they usually plan. Every user teaches Klacksy their own facts, and those
facts never surface in anyone else's conversation.

Each entry has a short title, its content, a category, an importance from 1 to 10, optional tags,
and a pinned flag. Pinned entries are always in context; unpinned ones are looked up when they fit
the question.

## Reminder notes

Separate from memory, Klacksy can hold a piece of information back and hand it over later — a
personal outbox. A note is stashed, later read out, and then archived. Notes already delivered stay
retrievable, so "what did you tell me yesterday" has an answer.

## What this is not

Memory is not the conversation history. History is what was said in the current thread; memory is
what survives when the thread ends. And memory is not Klacksy's character — how Klacksy speaks and
behaves is described by `explain_klacksy_personality`.

## Related skills

- `get_ai_memories` / `add_ai_memory` / `update_ai_memory` / `delete_ai_memory` — company memory (administrators)
- `add_personal_memory` — the signed-in user teaches Klacksy a fact about themselves
- `stash_pending_note` / `manage_pending_notes` / `recall_delivered_notes` — the reminder outbox

## Trigger phrases

- "What do you know about me?"
- "Remember that I always plan the Bern team."
- "Forget what you saved about the night shift."
- "Why do you still know that?"
- "Was hast du dir gemerkt?"
