# Identity Provider (LDAP, OAuth2, AD)

## Provider-Typen

| Type | Enum | Beschreibung |
|------|------|--------------|
| 0 | LDAP | Standard LDAP |
| 1 | ActiveDirectory | Windows AD |
| 2 | OAuth2 | OAuth 2.0 |
| 3 | OpenIdConnect | OIDC |

## LDAP Authentifizierung

### Flow

1. Login-Request (Email/Username + Password)
2. Lokale DB-Auth versuchen
3. Falls fehlgeschlagen: LDAP-Provider mit `UseForAuthentication=true` durchlaufen
4. Bei erfolgreichem LDAP-Bind: User in DB anlegen/finden
5. JWT-Token generieren

### Duplikat-Vermeidung

Neue Felder in `Client`:
- `IdentityProviderId` (Guid?)
- `LdapExternalId` (string?) - ObjectGuid oder DN aus LDAP

### BuildUserDn

```csharp
// LDAP: uid=username,baseDn
// AD: cn=username,baseDn
if (providerType == IdentityProviderType.ActiveDirectory)
    return $"cn={username},{baseDn}";
return $"uid={username},{baseDn}";
```

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

### Auth Fallback

```csharp
// Negotiate → Basic für OpenLDAP
catch (Exception ex) when (provider.Type == ActiveDirectory)
{
    connection.AuthType = AuthType.Basic;
    connection.Bind();
}
```

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
- `Domain/Interfaces/ILdapService.cs`
- `Domain/Models/Authentification/IdentityProvider.cs`

**Infrastructure:**
- `Infrastructure/Services/Identity/OAuth2Service.cs`
- `Infrastructure/Services/Identity/LdapService.cs`
- `Infrastructure/Services/Identity/ClientSyncService.cs`
- `Infrastructure/Services/Authentication/AuthenticationService.cs`

**Presentation:**
- `Presentation/Controllers/UserBackend/OAuth2Controller.cs`
