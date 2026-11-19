---
name: identity-provider
description: "Verwende wenn an LDAP, OAuth2, OpenID Connect Authentifizierung oder Identity-Provider-Sync gearbeitet wird"
---

# Identity Provider (LDAP, OAuth2, AD)

## Provider-Typen

| Type | Enum | Beschreibung |
|------|------|--------------|
| 0 | LDAP | Standard LDAP |
| 1 | ActiveDirectory | Windows AD |
| 2 | OAuth2 | OAuth 2.0 |
| 3 | OpenIdConnect | OIDC |

## LDAP Authentifizierung (Search-then-Bind)

### Flow

1. Login-Request (Email/Username + Password)
2. Lokale DB-Auth versuchen
3. Falls fehlgeschlagen: je Provider, für den `DirectoryLoginProviders.IsUsableForLogin` gilt (LDAP/AD, aktiv,
   `UseForAuthentication=true`, AD nur mit SSL), nach SortOrder. **AD-Login nur über LDAPS (636)**; AD ohne SSL wird nur
   gelesen (Negotiate, Import/Test), `BindAsUserAsync` wirft dort `DirectoryUnavailableException`:
   1. `ILdapService.FindLoginEntryAsync(provider, typed)`: Bind mit dem Service-Account, Subtree-Suche unter BaseDn
      (Filter `LdapUserFilters.ForLogin`, SizeLimit 2). Nur genau ein Treffer zählt; deaktivierte AD-Konten
      (`userAccountControl`) fallen heraus.
   2. `IAppUserReader.FindByDirectoryLinkAsync(provider.Id, entry.StableId)` liefert den verknüpften `AppUser`.
   3. Verknüpfter, aktiver und gesperrter (Lockout) Account: Abbruch ohne Bind.
   4. `BindAsUserAsync(provider, entry.DistinguishedName, password)` (neue Verbindung, Basic). LDAP-Fehler 49 = falsches
      Passwort; andere LDAP-Fehler = `DirectoryUnavailableException` (nächster Provider, kein Fehlversuch).
   5. Falsches Passwort: `AccessFailedAsync` auf dem verknüpften Account.
   6. Passwort richtig, aber Account nicht verknüpft oder deaktiviert: nach der Schleife
      `DirectoryAccessNotGrantedException` (401, ErrorCode `directoryAccessNotGranted`).
   7. Sonst: `ResetAccessFailedCountAsync`, Login als verknüpfter Account.
4. JWT-Token generieren

Es wird **kein Account automatisch angelegt** und keine E-Mail-Adresse aus dem getippten Text gemappt.

### Freigabeliste

- Admin-Endpunkte auf `IdentityProvidersController`: `GET IdentityProviders/{id}/DirectoryAccounts?search=` und
  `PUT IdentityProviders/{id}/DirectoryAccounts/Access` (`ExternalId`, `MayLogin`).
- Anhaken legt einen `AppUser` ohne Rolle und ohne Passwort an (`RegisterDirectoryUserAsync`) oder reaktiviert den
  bereits verknüpften Account (nur ohne Rolle, sonst `directoryAccountHasRoles`). Keine Verknüpfung per E-Mail: ist die
  E-Mail schon vergeben, antwortet der Server `directoryAccountEmailTaken`.
  Abhaken/Wiederanhaken laufen über `DeactivateAccountCommand` / `ReactivateAccountCommand`.
- Verknüpfung auf `AppUser`: `IdentityProviderId` + `DirectoryExternalId` (eindeutiger Index, FK SetNull).
- Verzeichnis-Accounts ohne `PasswordHash` bekommen keinen Passwort-Reset-Token.

### Externe ID

`LdapUserEntry.StableId` = `ObjectGuid ?? EntryUuid ?? DistinguishedName` (einzige Stelle der Regel). Dieselbe ID
nutzen Freigabeliste und Client-Import.

### Client-Import

- Paged Search (`PageResultRequestControl`, Seite 500); `LdapUserSearchResult.IsComplete` zeigt, ob alles gelesen wurde.
- `ClientSyncService` bricht bei unvollständigem oder leerem Ergebnis ab, ohne etwas zu schreiben
  (Mitgliedschaften werden nicht beendet).
- Re-Key: passt keine StableId, aber ein DN im Sync-Log, wird die Externe ID umgeschrieben statt zu duplizieren.

### Filter

`LdapFilterEncoder` (RFC 4515) und `LdapUserFilters` (`Base`, `ForLogin`, `ForListing`, `ForExternalId`) bauen alle
Filter; getippter Text wird nie roh in einen Filter oder DN gesetzt.

### Provider-Typen im Login

- Typ LDAP (auch Synology): Login-Attribute `uid`, `mail`.
- Typ ActiveDirectory: `sAMAccountName`, `userPrincipalName`, `mail`.

### Forumsys

Der geseedete Forumsys-Provider ist deaktiviert (`is_enabled=false`, `use_for_authentication=false`), damit keine
Passwörter im Klartext an den Testserver gehen.

### Duplikat-Vermeidung

Neue Felder in `Client`:
- `IdentityProviderId` (Guid?)
- `LdapExternalId` (string?) - `StableId` aus LDAP

## OAuth2 / OpenID Connect

### Architektur

```
Frontend → GET /authorize → Backend
    ↓
Redirect zu OAuth Provider
    ↓
Callback mit Code
    ↓
POST /callback → Token Exchange → UserInfo → JWT
```

### API Endpoints

| Endpoint | Methode | Beschreibung |
|----------|---------|--------------|
| `/api/backend/OAuth2/providers` | GET | Provider auflisten |
| `/api/backend/OAuth2/authorize/{id}` | GET | Auth URL generieren |
| `/api/backend/OAuth2/callback` | POST | Code gegen Token |
| `/api/backend/OAuth2/logout-url/{id}` | GET | Logout URL |

### State-Parameter

```
state = "{providerId}_{randomGuid}"
```

Beim Callback wird Provider-ID extrahiert.

## Active Directory

### Adress-Import

OpenLDAP verwendet `street` statt `streetAddress`:

```csharp
StreetAddress = GetStringAttribute(entry, "streetAddress")
             ?? GetStringAttribute(entry, "street")
```

### Gender via personalTitle

| personalTitle | Gender |
|---------------|--------|
| Herr, Mr, Monsieur, Signor | Male |
| Frau, Mrs, Ms, Madame, Signora | Female |
| Leer/unbekannt | Intersexuality |

### Auth-Typ (fest, kein Fallback)

- SSL: Basic über TLS (beide Typen)
- AD ohne SSL: Negotiate, nur lesend (Import/Test)
- LDAP ohne SSL: Basic

## Test-Server

### Forumsys LDAP

```
Host: ldap.forumsys.com
Port: 389
Base DN: dc=example,dc=com
Bind DN: cn=read-only-admin,dc=example,dc=com
Password: password
User: einstein, newton, tesla (pw: password)
```

## Dateien

**Domain:**
- `Domain/Interfaces/IOAuth2Service.cs`
- `Domain/Interfaces/Authentification/ILdapService.cs`
- `Domain/Models/Authentification/IdentityProvider.cs`

**Infrastructure:**
- `Infrastructure/Services/Identity/OAuth2Service.cs`
- `Infrastructure/Services/Identity/LdapService.cs`
- `Application/Services/Identity/ClientSyncService.cs`
- `Application/Services/Authentication/AuthenticationService.cs`

**Presentation:**
- `Presentation/Controllers/UserBackend/OAuth2Controller.cs`
