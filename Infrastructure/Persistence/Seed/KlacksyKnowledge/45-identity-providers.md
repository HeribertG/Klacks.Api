---
name: explain_identity_providers
description: |
  Explains signing in with an existing company directory or an external sign-in service: connecting
  a directory server by host, port, base entry and search filter, or a redirect-based service by
  client identifier, secret and the three endpoint addresses. Covers that this is added alongside
  the ordinary password login and never replaces it, that a person from a directory can sign in only
  after an administrator has ticked them in the approval list of the connection (no account is
  created automatically), that such an account arrives without any role and without a local
  password, which login names people type, that a company directory needs an encrypted connection
  (LDAPS) for sign-in, and why the staff import can stop. Use this when the user
  asks about single sign-on, connecting a company directory, why a colleague from the directory
  cannot sign in, or why a newly arrived person sees nothing.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  mul:
    - ldap
    - oauth
    - single sign-on
  de:
    - anmeldedienst
    - verzeichnisdienst
    - firmenverzeichnis
    - anmeldung über
  en:
    - identity provider
    - directory server
    - sign in with
synonyms:
  de: [identity provider, anmeldedienst, verzeichnisdienst, firmenverzeichnis, single sign-on, anmeldung mit firmenkonto, active directory anbinden, warum hat der neue benutzer keine rechte, kein zugriff verzeichnis anmeldung, mitarbeiter aus verzeichnis freigeben, benutzer aus active directory freischalten, ldap anmeldung funktioniert nicht]
  en: [identity provider, directory server, single sign-on, sign in with company account, connect active directory, new user has no permissions, no access directory sign-in, approve person from directory, allow directory user to sign in, ldap sign-in not working]
  fr: [fournisseur d identité, annuaire d entreprise, authentification unique, connexion avec compte d entreprise, autoriser une personne de l annuaire à se connecter, pas d accès annuaire]
  it: [provider di identità, directory aziendale, single sign-on, accesso con account aziendale, autorizzare una persona della directory all accesso, nessun accesso directory]
---

# Signing in through an existing directory or service

<!-- level:short -->

## Stage 1 — What this is for

Instead of maintaining a separate password in Klacks, people can sign in with the account they
already have. Four kinds of connection are supported
(de: "Identity Provider", fr: "Fournisseur d'identité", it: "Provider di identità"):

- **Directory server** and **company directory** — the classic on-premise directories. People type
  their usual name and password in the normal sign-in form.
- **OAuth 2.0** and **OpenID Connect** — redirect-based services. People click a button, authorise
  at the provider, and come back signed in.

**This is always an addition, never a replacement.** The ordinary password login stays available; a
connection cannot switch it off.

Each connection has two independent purposes, and either can be used on its own:

- **Use for authentication** — people may sign in through it. For a directory server or company
  directory this is **not enough on its own**: each person must also be approved by an administrator
  (see Stage 3).
- **Use for staff import** — staff records are read from it. This creates **staff entries**, not
  sign-in accounts.

<!-- level:elements -->

## Stage 2 — The fields

Card anchor: `identity-providers-card`.

**Always visible** — name, kind, enabled, sort order, and the two purpose switches above. Sort
order matters: directory connections are tried in that order, and the first success wins.

**Directory server / company directory** (tab de: "Verbindung", en: "Connection")

Host, port, whether the connection is encrypted, the base entry to search below, the account used
to search, its password, and the filter that selects people. Defaults are filled in: port 389, or
636 once encryption is switched on, and a common filter for person entries. Changing the kind fills
the filter default of the new kind, unless the filter was edited by hand.

The two kinds differ in more than the label: they accept different login names (see Stage 3) and use
different attribute names when searching. Klacks never guesses a person's entry from the typed name.
It first looks the person up with the search account, then checks the password against the entry it
found. The LDAP server package of a NAS (for example a Synology "LDAP Server") is the kind **directory
server**; a Synology "Directory Server" (Active Directory compatible) is the **company directory** kind.

**Company directory: sign-in only with encryption.** A company directory refuses to check a person's
password over an unencrypted connection. Sign-in through the company-directory kind therefore works
**only with encryption switched on (LDAPS, port 636)**. Without it the connection can still be read for
the staff import and the connection test only when Klacks runs directly on Windows (it then signs the
search account in with the Windows procedure); in Docker (Linux) this procedure is not supported, so
Active Directory needs LDAPS for everything there, including import and test. Nobody can sign in through it,
the sign-in approval tab stays closed, and the dialog shows a warning. The message "Strong authentication required" from the server means: switch on
encryption. For LDAPS:
- the **host** must be the server's full name exactly as written in its certificate (for example
  `dc1.firma.local`), not an IP address;
- the computer running Klacks must trust the certificate's issuing authority. On Windows import the CA
  certificate into the trusted root store (`certutil -addstore Root ca.cer`). In Docker mount the CA as a
  PEM file, set `LDAPTLS_CACERT` to its path and add the host name with `extra_hosts` if DNS does not
  resolve it. A domain controller with a self-signed certificate sends no CA chain: then its own
  certificate as PEM file is the trust anchor. Without a trusted certificate the connection is refused
  and shows as a connection error;
- enter the search account as user principal name (for example `klacks-search@firma.local`).
Klacks never accepts an untrusted certificate.

A plain **directory server** connection without encryption works, but the dialog warns that passwords
then travel unencrypted.

**Sign-in approval tab** (de: "Anmelde-Freigabe", en: "Sign-in approval", fr: "Autorisation de connexion", it: "Autorizzazione accesso") — a saved directory connection that is set to be used for
authentication gets one more tab next to the connection tab. It lists the people found in the directory,
each with a checkbox (de: "Darf sich anmelden", en: "May sign in", fr: "Peut se connecter", it: "Può accedere") that lets the person sign in (Stage 3). It does not exist for a new, unsaved connection and not for the
redirect-based kinds. The list searches the directory live and shows at most 1000 people; a notice
says when it was cut off, and typing a name narrows the search.

**OAuth 2.0 / OpenID Connect** (tab "OAuth")

Client identifier, client secret, the address people are sent to, the address where the token is
fetched, the address where the person's details are read, the requested scopes, and a tenant
identifier where the service needs one.

Both kinds read the person's details from the details endpoint. The difference between them is a
sign-out address that is derived for the second kind only.

**Testing** — the dialog can test the connection and report how many active people were found, with a
few examples, before anything is saved. Disabled accounts and the company directory's built-in system
accounts (Administrator, Guest, krbtgt) are not counted. A "+" after the number means the server stopped
early because of its own result limit; the real number is higher.

**No field is enforced by the server.** All connection fields are optional in the data model; a
connection can be saved incomplete and will then simply fail at sign-in. The test button is the way
to find that out.

<!-- level:effects -->

## Stage 3 — Who may sign in, and what happens

**Nobody is created automatically.** Being found in the directory is not enough. A person can sign in
through a directory connection only after an administrator has **ticked** them in the sign-in approval
tab of that connection (Stage 2).

- **Ticking** creates a Klacks login account for that person: first and last name from the directory,
  the e-mail address from the directory (its mail field, otherwise its user principal name), already
  confirmed. **No mail is sent, there is no welcome or reset link, and the account has no role and no
  local password.** The person signs in with the directory password and sees only what a person without
  a role may see; the rights are granted afterwards in user administration, which marks such an account
  as (de: "Nur Verzeichnis-Anmeldung", en: "Directory sign-in only", fr: "Connexion par annuaire uniquement", it: "Solo accesso tramite directory")
  and offers no password link (de: "Passwort zurücksetzen", en: "Send Access Credentials", fr: "Envoyer les informations d'accès", it: "Invia Credenziali di Accesso") for it.
- A person without a usable e-mail address in the directory cannot be ticked; the row says why.
- **An existing Klacks account is never taken over.** Klacks does not link by e-mail address. When the
  directory person's e-mail address is already used by any Klacks account, the row says so (without
  naming that account) and the tick is refused. To let the person in, first **delete that Klacks account
  or change its e-mail address** in user administration, then tick again; a new account without a role is
  created.
- **Unticking** switches the account off exactly like deactivating it: the person can no longer sign
  in, the data stays. The rules of deactivating apply: administrators cannot untick their own account,
  and the last administrator cannot be switched off. Unticking a person who has no Klacks account is
  refused.
- **Re-ticking** switches the same account on again, but only while it has **no role**. A switched-off
  account that still holds a role (for example an administrator) cannot be switched on from this list;
  the row says so, and it is reactivated in user administration instead.
- **When someone leaves**, disabling the person in the directory stops new sign-ins, but a session that
  is already running continues until it expires. **Remove the tick in Klacks as well**, so the account is
  switched off at once.
- Approvals stay when a connection is switched off, but nobody signs in through a connection that is
  off. Deleting a connection keeps the accounts; nobody signs in through a deleted connection. When the
  same directory is connected again and the person is ticked there, the existing account is taken over
  by the new connection instead of creating a second one (only for directories that deliver a stable
  identifier, see below), but only while that account has no role; an account with a role is refused and
  must be handled in user administration.

**The stable identifier matters.** Klacks recognises a directory person by the directory's own
unchangeable identifier: objectGUID in a company directory, entryUUID on a directory server. Make sure the
directory delivers it (Synology and OpenLDAP do; the search account must be allowed to read it). Without
it, Klacks falls back to the person's full directory path (distinguished name); that fallback is weak:
moving or renaming the person in the directory breaks the approval and the staff link.

**Which names people type.** Directory server: the user identifier (uid) or the e-mail address.
The approval list shows exactly this login name.
Company directory: the account name (sAMAccountName), the user principal name or the e-mail address.
Only a single, unambiguous match counts; a name that matches several people signs nobody in. A person
who is disabled in the company directory counts as not found. Typing the e-mail address of some Klacks
account does not sign in to that account: the typed text is only used to find the person in the
directory.

**What the person sees.** A wrong directory password gives the ordinary "wrong name or password" and
counts as a failed attempt on the linked account, so repeated failures lock it like a local account.
When the directory password is right but the person is not approved (never ticked, unticked, or switched
off; also when the person types the e-mail address of the switched-off account), the sign-in page says there is **no access and an administrator should be contacted**. That
message appears only after the directory accepted the password, so it reveals nothing about who exists
in the directory. When the directory cannot be reached, the next connection is tried. The local password
check always runs first. The installation's built-in setup administrator can never sign in through a
directory.

**Why can't the new colleague sign in?** Check in this order:

1. Is the connection saved, enabled and set to be used for authentication? For a company directory: is
   encryption (LDAPS, port 636) switched on, with the host written as in the certificate?
2. Is the person ticked in the sign-in approval tab, and is the account still active in user administration?
   Is the tick refused because the e-mail address is already used by a Klacks account? Then delete that
   account or change its e-mail address first.
3. Is the typed name one of the accepted login names above, and is the person disabled in the company
   directory?
4. Does the person see "no access"? Then the directory password was right and only the approval is
   missing.
5. Does the person see an empty screen after signing in? Then everything works, and the account has no
   role yet: grant one in user administration.

After an update that introduced this approval, directory sign-in stops for everyone until an
administrator has ticked them.

**Staff import.** Only active people are imported: disabled accounts and the company directory's
built-in system accounts are never imported, and a person who is disabled in the directory later ends
up like someone who left (the membership is ended). The directory is read page by page. When it delivers an incomplete or an empty list,
for example because the server caps the number of results (a common default is 500), the whole import
is **aborted and nothing is changed**, instead of ending the memberships of everyone who is missing; the
error is shown on the connection. Raise the server's result limit or narrow the filter, then run it
again. Staff are matched by the directory's stable identifier, not by a name that can change, so a
renamed person is not duplicated. The import creates staff entries only, never sign-in accounts.

**Redirect-based sign-in** (OAuth 2.0, OpenID Connect) has no approval list. The match is by e-mail
address only, against an account that **already exists**; nothing is created. A service that returns no
e-mail address cannot sign anybody in.

**Passwords on the wire.** A plain directory-server connection on port 389 sends the password
unencrypted to the directory server. Switch on encryption (port 636) wherever the server supports it.
For a company directory, encryption is required for sign-in anyway (see Stage 2).

Stored secrets — the directory search password and the client secret — are encrypted, and both are
returned masked once saved. Re-saving with the mask in place keeps the stored value.

## Related skills

- `list_identity_providers`, `create_identity_provider`, `update_identity_provider`, `delete_identity_provider`
- `test_identity_provider_connection`
- `explain_user_account_creation` (how an account gets its first password)

## Trigger phrases

- "Can people sign in with their Windows account?"
- "Wie binde ich unser Firmenverzeichnis an?"
- "Der neue Mitarbeiter kann sich anmelden, sieht aber nichts."
- "Der Kollege aus dem Firmenverzeichnis kommt nicht rein: Kein Zugriff."
- "Why does a person from our directory get 'no access' although the password is right?"
- "Does Klacks create an account for everyone in Active Directory automatically?"
- "Muss ich die Passwörter dann noch in Klacks pflegen?"
- "Werden die Gruppen aus dem Verzeichnis übernommen?"
