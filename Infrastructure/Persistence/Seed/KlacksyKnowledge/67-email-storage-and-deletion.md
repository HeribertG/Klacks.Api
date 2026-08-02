---
name: explain_email_storage_and_deletion
description: |
  Explains the difference between the mailbox on the mail server and the copy Klacks keeps of a
  message, and why deleting, permanently deleting, restoring or moving a message does not change
  both sides equally. Covers what happens when an entire mail folder is deleted, where the two
  sides diverge furthest, and what happens when a message vanishes on the server outside Klacks.
  Use this when a deleted email may not be truly gone, or a message disappeared unexpectedly.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - imap
  de:
    - mail wirklich gelöscht
    - ordner löschen mail
    - mail verschwunden
  en:
    - is the email really gone
    - delete a mail folder
    - email disappeared
synonyms:
  de: [ist die mail wirklich weg, warum ist die mail plötzlich verschwunden, was passiert beim löschen eines ordners, bleibt eine gelöschte mail noch in der datenbank, mail extern gelöscht was passiert in klacks, unterschied mailserver und klacks speicher, warum kann ich die mail nach ordner-löschung nicht wiederherstellen]
  en: [is the email really gone, why did the email suddenly disappear, what happens when a mail folder is deleted, does a deleted email still exist in the database, email deleted externally what happens in klacks, difference between mail server and klacks storage, why can't i restore this email after deleting the folder]
  fr: [l'e-mail a-t-il vraiment disparu, que se passe-t-il en supprimant un dossier de messagerie, l'e-mail supprimé existe-t-il encore, différence entre serveur de messagerie et stockage klacks]
  it: [l'email è davvero sparita, cosa succede eliminando una cartella di posta, l'email eliminata esiste ancora nel database, differenza tra server di posta e archivio klacks]
---

# Email storage and deletion — the mail server and Klacks are two separate stores

## Core idea (one sentence)

Every action on a message touches two independent places — the mailbox on the mail server and
Klacks's own stored copy — and they do not always move together, or at the same speed.

## What each action really does

| Action | Klacks's own copy | The mail server |
|---|---|---|
| Delete (to trash) | Relabeled to the trash folder; the row itself stays fully intact and visible | Message moved into the server's own trash mailbox |
| Restore | Relabeled to the first available real folder — **not necessarily the original one** | Moved back to that same folder on the server |
| Permanently delete (trash only) | Row is marked deleted — invisible from that moment on, then physically purged once the company-wide retention period expires, like any other deleted record | Message is irreversibly expunged — gone for good |
| Delete a folder | All its rows are relabeled to trash — still visible, but the message behind them is already gone on the server | The folder **and everything still inside it** is deleted outright on the server |
| Message vanishes on the server | The next background fetch notices the gap and marks the row deleted immediately — skipping the trash step entirely | Already gone, by definition |

## The surprise

The everyday **delete** never marks anything "deleted" in Klacks's own sense — it is only a folder
move, exactly like any other move, fully reversible. **Permanently delete** is the one and only
action that flips that marker locally — yet even then the row is not instantly gone: it is hidden
from every view from that point on, and only physically erased later, when the company-wide
retention period for deleted records runs out (same rule as everywhere else in Klacks, not
email-specific).

## The folder-deletion case — where both sides disagree the most

Deleting a folder is the one action where the two stores end up in genuinely different states.
Locally, its messages are merely relabeled into trash — they look present and restorable. On the
mail server, though, the folder is deleted together with every message the server still had inside
it, with no server-side trash detour at all. A **Restore** attempted afterwards will look
successful in Klacks (the label flips back), while nothing happens on the server, because there is
no longer a message there to move. This follows from how every one of these actions is built: the
local change is saved first, and the mail-server call runs afterwards on a best-effort basis — a
failure there is only logged, never shown, and never undoes the local change.

## The reverse case

Delete a message directly on the mail server (or in another mail program) and the next scheduled
fetch — or an immediate refresh — notices its identifier is gone from that folder and marks the
local row deleted right away, skipping the trash step Klacks uses for its own delete button. There
is no warning; the message simply stops appearing on the next refresh.

## Related skills

`delete_email`, `restore_email`, `move_email_to_folder`, `list_email_folders`

## Trigger phrases

- "Ist die E-Mail nach dem Löschen wirklich weg?"
- "Why did this email disappear without me deleting it?"
- "Was passiert mit den Mails, wenn ich den Ordner lösche?"
