# Identity Provider Implementation Status

## Stand: 2026-01-10

---

## Übersicht

| Feature | Status | Getestet |
|---------|--------|----------|
| LDAP Client-Import | ✅ Abgeschlossen | ✅ Ja |
| LDAP Duplikat-Vermeidung | ✅ Abgeschlossen | ✅ Ja |
| LDAP Authentifizierung | ✅ Abgeschlossen | ✅ Ja |
| OAuth2 Authentifizierung | ✅ Abgeschlossen | ✅ Ja |
| OpenID Connect | ✅ Abgeschlossen | ✅ Ja |
| SSO Logout | ✅ Abgeschlossen | ✅ Ja |
| OAuth2 CQRS Refactoring | ✅ Abgeschlossen | ✅ Ja |
| Active Directory | ⏳ Ausstehend | - |

---

## 1. LDAP Implementation

### 1.1 Client-Import (Duplikat-Vermeidung)

**Problem**: Mehrfaches Synchronisieren erzeugte Duplikate.

**Lösung**: Neue Felder in Client-Tabelle:
- `IdentityProviderId` (Guid?) - Referenz zum Identity Provider
- `LdapExternalId` (string?) - ObjectGuid oder DistinguishedName aus LDAP

**Dateien**:
- `Domain/Models/Staffs/Client.cs` - Neue Felder
- `Application/Interfaces/IClientRepository.cs` - `GetByLdapExternalIdAsync()`
- `Infrastructure/Repositories/ClientRepository.cs` - Implementierung
- `Infrastructure/Services/Identity/ClientSyncService.cs` - Duplikat-Prüfung

**Migration**: `20260109071126_AddLdapFieldsToClient`

**Test**: 5x Synchronisierung mit Forumsys LDAP - keine Duplikate erstellt.

### 1.2 LDAP Authentifizierung

**Funktion**: Benutzer können sich mit LDAP-Credentials einloggen.

**Flow**:
1. Login-Request kommt rein (Email/Username + Password)
2. Zuerst lokale Datenbank-Authentifizierung versuchen
3. Falls fehlgeschlagen: Alle LDAP-Provider mit `UseForAuthentication=true` durchlaufen
4. Bei erfolgreichem LDAP-Bind: User in lokaler DB anlegen/finden
5. JWT-Token generieren und zurückgeben

**Dateien**:
- `Infrastructure/Services/Authentication/AuthenticationService.cs`
  - `ValidateLdapCredentialsAsync()` - Prüft alle LDAP-Provider
  - `GetOrCreateLdapUserAsync()` - Erstellt/findet lokalen User
- `Infrastructure/Services/Identity/LdapService.cs`
  - `BuildUserDn()` - Baut DN aus Username automatisch auf
- `Application/Validation/Accounts/LoginUserQueryValidator.cs`
  - Email-Validierung entfernt (erlaubt nun Usernamen)
- `Presentation/DTOs/Registrations/LogInResource.cs`
  - `[EmailAddress]` Annotation entfernt

**Test**: Login mit `einstein` / `password` gegen Forumsys LDAP erfolgreich.

### 1.3 Handler-Refactoring

Services werden im Repository initiiert (nicht im Handler):

```
Handler → Repository → Services (korrekt)
Handler → Services (falsch)
```

**Geänderte Handler**:
- `TestConnectionCommandHandler` → `_repository.TestConnectionAsync()`
- `SyncClientsCommandHandler` → `_repository.SyncClientsAsync()`

**Zirkuläre Abhängigkeit behoben**: Ungenutztes `IIdentityProviderRepository` aus `ClientSyncService` entfernt.

---

## 2. OAuth2 / OpenID Connect Implementation

### 2.1 Architektur

```
Frontend                    Backend                         OAuth Provider
    |                          |                                  |
    |-- GET /providers ------->|                                  |
    |<-- [provider list] ------|                                  |
    |                          |                                  |
    |-- GET /authorize ------->|                                  |
    |<-- {authUrl, state} -----|                                  |
    |                          |                                  |
    |-------- redirect --------|--------------------------------->|
    |<------- callback --------|-- (code, state) -----------------|
    |                          |                                  |
    |-- POST /callback ------->|-- exchange code for token ------>|
    |                          |<-- {access_token, id_token} -----|
    |                          |                                  |
    |                          |-- GET userinfo ----------------->|
    |                          |<-- {email, name, ...} -----------|
    |                          |                                  |
    |<-- {jwt, user info} -----|                                  |
```

### 2.2 Implementierte Komponenten

**Interface**: `Domain/Interfaces/IOAuth2Service.cs`
```csharp
public interface IOAuth2Service
{
    string GetAuthorizationUrl(IdentityProvider provider, string redirectUri, string state);
    Task<OAuth2TokenResponse?> ExchangeCodeForTokenAsync(IdentityProvider provider, string code, string redirectUri);
    Task<OAuth2UserInfo?> GetUserInfoAsync(IdentityProvider provider, string accessToken);
}
```

**Service**: `Infrastructure/Services/Identity/OAuth2Service.cs`
- Verwendet `IHttpClientFactory` für HTTP-Requests
- Unterstützt Standard OAuth2 Authorization Code Flow
- Parst UserInfo-Response mit Fallback für verschiedene Claim-Namen

**Controller**: `Presentation/Controllers/UserBackend/OAuth2Controller.cs`

| Endpoint | Methode | Beschreibung |
|----------|---------|--------------|
| `/api/backend/OAuth2/providers` | GET | Listet alle OAuth2/OIDC Provider |
| `/api/backend/OAuth2/authorize/{providerId}` | GET | Generiert Authorization URL |
| `/api/backend/OAuth2/callback` | POST | Verarbeitet Callback, tauscht Code gegen Token |
| `/api/backend/OAuth2/logout-url/{providerId}` | GET | Generiert Logout URL (falls unterstützt) |

### 2.3 CQRS-Struktur (Clean Architecture)

Der OAuth2Controller folgt dem Handler → Repository → DbContext Pattern:

**Queries** (`Application/Queries/OAuth2/`):
| Query | Beschreibung |
|-------|--------------|
| `GetOAuth2ProvidersQuery` | Lädt alle OAuth2/OIDC Provider |
| `GetOAuth2AuthorizeQuery` | Generiert Authorization URL für Provider |
| `GetOAuth2LogoutUrlQuery` | Generiert Logout URL für Provider |

**Commands** (`Application/Commands/OAuth2/`):
| Command | Beschreibung |
|---------|--------------|
| `OAuth2CallbackCommand` | Verarbeitet OAuth2 Callback, tauscht Code gegen Token |

**Handlers** (`Application/Handlers/OAuth2/`):
| Handler | Beschreibung |
|---------|--------------|
| `GetOAuth2ProvidersQueryHandler` | Lädt Provider via Repository |
| `GetOAuth2AuthorizeQueryHandler` | Generiert Auth URL via OAuth2Service |
| `GetOAuth2LogoutUrlQueryHandler` | Generiert Logout URL via OAuth2Service |
| `OAuth2CallbackCommandHandler` | Token-Exchange und User-Erstellung |

**DTOs** (`Presentation/DTOs/OAuth2/`):
| DTO | Beschreibung |
|-----|--------------|
| `OAuth2ProviderResource` | Provider-Info für Frontend |
| `OAuth2AuthorizeResponse` | Authorization URL + State |
| `OAuth2LogoutUrlResponse` | Logout URL + SupportsLogout Flag |
| `OAuth2CallbackRequest` | Code, State, RedirectUri vom Frontend |

**DI-Registration**: `Infrastructure/Extensions/ServiceCollectionExtensions.cs`
```csharp
services.AddScoped<IOAuth2Service, Services.Identity.OAuth2Service>();
```

### 2.4 State-Parameter Handling

Der State-Parameter codiert die Provider-ID für stateless Betrieb:
```
state = "{providerId}_{randomGuid}"
```

Beim Callback wird die Provider-ID extrahiert um den richtigen Provider zu laden.

### 2.5 Provider-Typen

| Type | Enum | Beschreibung |
|------|------|--------------|
| 0 | LDAP | Standard LDAP |
| 1 | ActiveDirectory | Windows AD |
| 2 | OAuth2 | OAuth 2.0 |
| 3 | OpenIdConnect | OIDC (OAuth2 + ID Token) |

### 2.6 Synology SSO (Development)

| Einstellung | Wert |
|-------------|------|
| Synology IP | 192.168.1.163:5001 |
| App Name | KlacksHTTPS |
| Redirect URI | https://127.0.0.1:4200/oauth2/callback |
| Frontend starten | `npm run start:ssl` (HTTPS erforderlich) |

**Einschränkung**: Synology SSO unterstützt kein externes Logout (kein `end_session_endpoint` in OIDC Discovery). Der Logout-Endpoint gibt `supportsLogout: false` zurück.

### 2.7 Getestete Szenarien

- ✅ Provider-Liste abrufen (Type 2 und 3 werden gelistet)
- ✅ Authorization URL generieren (alle OAuth2-Parameter korrekt)
- ✅ Callback-Verarbeitung (State parsing, Provider-Lookup, Token-Exchange)
- ✅ Synology SSO Login (HTTPS mit selbstsigniertem Zertifikat)
- ✅ Logout-URL Generierung (mit Provider-spezifischer Unterstützung)
- ✅ Fehlerbehandlung bei ungültigen Credentials (401 von Google erwartet)

---

## 3. Offene Arbeiten

### 3.1 Active Directory

**Status**: Ausstehend (erfordert Azure AD Free Tier für Tests)

**Geplante Features**:
- Integration mit Azure AD / Entra ID
- LDAP-ähnliche Authentifizierung über AD
- Gruppenimport aus AD

---

## 4. Test-Server

### 4.1 Forumsys LDAP (funktioniert)

```
Host: ldap.forumsys.com
Port: 389
Base DN: dc=example,dc=com
Bind DN: cn=read-only-admin,dc=example,dc=com
Bind Password: password
User Filter: (objectClass=person)
```

**Test-User**: `einstein`, `newton`, `tesla` (Password: `password`)

### 4.2 Zflexldap (offline)

Server ist offline - Tests als `[Ignore]` markiert.

---

## 5. Bekannte Einschränkungen

1. **Build bei laufendem Backend**: Backend muss vor dem Build gestoppt werden (Datei-Lock)
2. **OAuth2 ohne echte Credentials**: Vollständiger Flow nur mit registrierter OAuth2-App testbar
3. **AD ohne Azure**: Active Directory Tests erfordern Azure AD Free Tier

---

## 6. Dateien-Übersicht

### Domain
- `Domain/Interfaces/IOAuth2Service.cs`
- `Domain/Interfaces/ILdapService.cs`
- `Domain/Models/Authentification/IdentityProvider.cs`
- `Domain/Models/Staffs/Client.cs` (LdapExternalId, IdentityProviderId)

### Infrastructure
- `Infrastructure/Services/Identity/OAuth2Service.cs`
- `Infrastructure/Services/Identity/LdapService.cs`
- `Infrastructure/Services/Identity/ClientSyncService.cs`
- `Infrastructure/Services/Authentication/AuthenticationService.cs`
- `Infrastructure/Repositories/IdentityProviderRepository.cs`
- `Infrastructure/Extensions/ServiceCollectionExtensions.cs`

### Presentation
- `Presentation/Controllers/UserBackend/OAuth2Controller.cs`
- `Presentation/DTOs/Registrations/LogInResource.cs`

### Application
- `Application/Validation/Accounts/LoginUserQueryValidator.cs`

### Migrations
- `20260109071126_AddLdapFieldsToClient.cs`
