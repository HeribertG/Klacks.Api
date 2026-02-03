# Database Connection

## Connection Details

| Umgebung | Host | Port | DB | User | Password |
|----------|------|------|----|------|----------|
| Development | localhost | 5432 | klacks | admin | admin |
| Integration Tests | localhost | 5434 | klacks | postgres | admin |

## Connection Strings

**Development:**
```
Host=localhost;Port=5432;Database=klacks;Username=admin;Password=admin
```

**Integration Tests:**
```
Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin
```

## Environment Variable

```bash
export DATABASE_URL="Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin"
```

## Datenbankzugriff via PowerShell (aus WSL)

```bash
# SQL-Datei ausführen
powershell.exe -Command "\$env:PGPASSWORD='admin'; & 'C:\\Program Files\\PostgreSQL\\17\\bin\\psql.exe' -h localhost -p 5434 -U postgres -d klacks -f 'C:\\SourceCode\\path\\to\\file.sql'"

# SQL-Query ausführen
powershell.exe -Command "\$env:PGPASSWORD='admin'; & 'C:\\Program Files\\PostgreSQL\\17\\bin\\psql.exe' -h localhost -p 5434 -U postgres -d klacks -c 'SELECT * FROM work LIMIT 5'"
```

## Client-bezogene Tabellen

| Tabelle | Beschreibung |
|---------|--------------|
| `client` | Haupt-Client-Tabelle |
| `membership` | Gültigkeitszeitraum |
| `address` | Adressen |
| `communication` | Telefon/E-Mail |
| `annotation` | Notizen |
| `client_image` | Profilbilder |
| `client_contract` | Vertragszuordnungen |
| `group_item` | Gruppenzugehörigkeit |

## Integration Tests ausführen

```bash
# Alle Tests
dotnet test Klacks.IntegrationTest/Klacks.IntegrationTest.csproj

# Spezifische Kategorie
dotnet test Klacks.IntegrationTest/Klacks.IntegrationTest.csproj --filter "Category=RealDatabase"

# Mit Environment Variable
DATABASE_URL="Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin" dotnet test Klacks.IntegrationTest/Klacks.IntegrationTest.csproj
```

## Test-Cleanup

Alle Test-Clients haben Präfix `INTEGRATION_TEST_`:

```sql
DELETE FROM client WHERE first_name LIKE 'INTEGRATION_TEST_%';
```

## Usage in Tests (Code-Beispiel)

```csharp
[OneTimeSetUp]
public void OneTimeSetUp()
{
    _connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";
}

[SetUp]
public async Task SetUp()
{
    var options = new DbContextOptionsBuilder<DataBaseContext>()
        .UseNpgsql(_connectionString)
        .Options;

    var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
    _context = new DataBaseContext(options, mockHttpContextAccessor);
}
```
