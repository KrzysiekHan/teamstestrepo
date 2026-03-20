# CLAUDE.md

This file provides guidance to AI assistants (Claude and others) working within this repository. Keep it up to date as the project evolves.

---

## Repository Overview

**Repository:** KrzysiekHan/teamstestrepo
**Language:** C# / .NET 8
**Type:** ASP.NET Core Web API — Microsoft Teams Approval Bot

### Cel aplikacji

Aplikacja zastępuje ręczny proces zatwierdzania. Przepływ:

1. **Zewnętrzny system** wywołuje endpoint tej aplikacji (`POST /approvals`), przekazując dane procesu i identyfikator pracownika
2. **Aplikacja** wysyła do wskazanego pracownika wiadomość w Microsoft Teams w formie Adaptive Card z dwoma polami do wypełnienia oraz przyciskami **Zatwierdź** / **Odrzuć**
3. **Pracownik** wypełnia formularz i klika przycisk
4. **Aplikacja** wywołuje endpoint zewnętrznego systemu, przekazując decyzję (approve/reject) wraz z wypełnionymi polami

Użytkownicy są zarządzani przez **Microsoft Entra ID** (Azure AD) — brak lokalnej bazy użytkowników.

---

## Architektura

```
Zewnętrzny system
      │
      │  POST /approvals  {userId, processId, ...}
      ▼
┌─────────────────────────────┐
│   ASP.NET Core Web API      │
│   (ta aplikacja)            │
│                             │
│  ┌─────────────────────┐    │
│  │  Approvals          │    │
│  │  Controller         │    │
│  └────────┬────────────┘    │
│           │                 │
│  ┌────────▼────────────┐    │
│  │  ApprovalService    │    │◄──── Microsoft Entra ID
│  └────────┬────────────┘    │      (autentykacja, tożsamość)
│           │                 │
│  ┌────────▼────────────┐    │
│  │  TeamsNotifier      │    │──── Bot Framework / Graph API
│  └─────────────────────┘    │     → Adaptive Card do pracownika
└─────────────────────────────┘
      │
      │  POST {callbackUrl}  {decision, field1, field2}
      ▼
Zewnętrzny system
```

---

## Project Structure

```
teamstestrepo/
├── CLAUDE.md
├── README.md
├── teamstestrepo.sln
├── src/
│   └── TeamstestRepo.Api/
│       ├── Controllers/
│       │   ├── ApprovalsController.cs     # POST /approvals — przyjmuje żądania z zewnątrz
│       │   └── BotController.cs           # POST /api/messages — endpoint Bot Framework
│       ├── Models/
│       │   ├── ApprovalRequest.cs         # DTO z zewnętrznego systemu
│       │   ├── ApprovalDecision.cs        # DTO wysyłane z powrotem do zewnętrznego systemu
│       │   └── AdaptiveCardPayload.cs     # Dane z formularza Teams
│       ├── Services/
│       │   ├── IApprovalService.cs
│       │   ├── ApprovalService.cs         # Logika biznesowa procesu zatwierdzania
│       │   ├── ITeamsNotifier.cs
│       │   └── TeamsNotifier.cs           # Wysyłanie proaktywnych wiadomości do Teams
│       ├── Cards/
│       │   └── ApprovalCard.json          # Szablon Adaptive Card
│       ├── Middleware/
│       │   └── ApiKeyMiddleware.cs        # Weryfikacja klucza API dla /approvals
│       ├── appsettings.json
│       ├── appsettings.Development.json   # git-ignored, lokalne sekrety
│       └── Program.cs
└── tests/
    ├── TeamstestRepo.UnitTests/
    │   ├── ApprovalServiceTests.cs
    │   └── TeamsNotifierTests.cs
    └── TeamstestRepo.IntegrationTests/
        └── ApprovalsEndpointTests.cs
```

---

## Tech Stack

| Warstwa | Technologia |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| Bot | Microsoft Bot Framework SDK (`Microsoft.Bot.Builder`) |
| Teams karty | Adaptive Cards |
| Tożsamość użytkowników | Microsoft Entra ID (Azure AD) |
| Graph API | `Microsoft.Graph` SDK — wysyłanie proaktywnych wiadomości |
| Autentykacja bota | Azure Bot Service (App Registration) |
| Testy | xUnit + Moq + FluentAssertions |
| Format odpowiedzi | RFC 7807 Problem Details |

---

## Kluczowe przepływy

### 1. Inicjowanie zatwierdzenia (inbound)

```
POST /approvals
Authorization: ApiKey {secret}
{
  "processId": "...",
  "userId": "user@company.com",   // Entra ID UPN lub objectId
  "callbackUrl": "https://...",   // gdzie odesłać decyzję
  "field1Label": "Komentarz",
  "field2Label": "Numer zlecenia"
}
```

- Aplikacja waliduje klucz API w middleware
- Pobiera Teams `conversationReference` dla użytkownika przez Microsoft Graph
- Wysyła Adaptive Card jako proaktywną wiadomość

### 2. Odpowiedź pracownika (Teams → bot)

Bot Framework dostarcza payload do `POST /api/messages`:

```json
{
  "type": "invoke",
  "value": {
    "processId": "...",
    "decision": "approve",
    "field1": "wartość wpisana przez pracownika",
    "field2": "wartość wpisana przez pracownika"
  }
}
```

### 3. Odesłanie decyzji (outbound)

Aplikacja wywołuje `callbackUrl` z zewnętrznego żądania:

```
POST {callbackUrl}
{
  "processId": "...",
  "decision": "approve" | "reject",
  "decidedBy": "user@company.com",
  "decidedAt": "2026-03-20T15:00:00Z",
  "field1": "...",
  "field2": "..."
}
```

---

## Konfiguracja

### appsettings.json (niesekrety)

```json
{
  "BotFramework": {
    "AppId": "",
    "TenantId": ""
  },
  "ExternalApi": {
    "TimeoutSeconds": 30
  }
}
```

### Sekrety (Secret Manager / zmienne środowiskowe)

| Klucz | Opis |
|---|---|
| `BotFramework:AppSecret` | Sekret App Registration bota |
| `ApiKey:InboundSecret` | Klucz API do endpointu `/approvals` |
| `Graph:ClientSecret` | Sekret do Microsoft Graph (jeśli oddzielny) |

```bash
# Lokalna konfiguracja sekretów
dotnet user-secrets init --project src/TeamstestRepo.Api
dotnet user-secrets set "BotFramework:AppSecret" "..." --project src/TeamstestRepo.Api
dotnet user-secrets set "ApiKey:InboundSecret" "..." --project src/TeamstestRepo.Api
```

---

## Commands

### Setup

```bash
git clone <repo-url>
cd teamstestrepo
dotnet restore
```

### Development

```bash
# Uruchom API
dotnet run --project src/TeamstestRepo.Api

# Hot reload
dotnet watch run --project src/TeamstestRepo.Api

# Tunelowanie dla Bot Framework (wymagane do lokalnych testów z Teams)
# ngrok http 5000  lub  devtunnel host -p 5000
# Następnie zaktualizuj Messaging Endpoint w Azure Bot: https://<tunnel>/api/messages
```

### Testing

```bash
dotnet test
dotnet test --logger "console;verbosity=detailed"
dotnet test tests/TeamstestRepo.UnitTests
```

### Linting & Formatting

```bash
dotnet format --verify-no-changes   # sprawdź
dotnet format                        # napraw
dotnet build --warnaserror           # build z ostrzeżeniami jako błędy
```

### Build

```bash
dotnet build
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

---

## Code Conventions

### Nazewnictwo (standardy Microsoft C#)

- **Klasy, metody, właściwości, zdarzenia:** `PascalCase`
- **Lokalne zmienne, parametry:** `camelCase`
- **Prywatne pola:** `_camelCase`
- **Interfejsy:** prefix `I` — `IApprovalService`, `ITeamsNotifier`
- **Metody async:** sufiks `Async` — `SendApprovalCardAsync()`, `HandleDecisionAsync()`

### Wzorce architektury

- Kontrolery są **cienkie** — tylko routing, walidacja wejścia, delegacja do serwisów
- Logika biznesowa w serwisach; serwisy rejestrowane przez DI jako `Scoped`
- `TeamsNotifier` jest odpowiedzialny **wyłącznie** za komunikację z Teams/Bot Framework
- `ApprovalService` jest odpowiedzialny **wyłącznie** za logikę procesu zatwierdzania
- Modele (`Models/`) to czyste DTO — brak logiki, brak atrybutów EF

### Async / Await

- Zawsze `async`/`await` dla operacji I/O (HTTP calls do Graph, do zewnętrznego systemu)
- **Nigdy** `.Result` ani `.Wait()` — grozi deadlockiem
- Wszystkie publiczne metody async przyjmują `CancellationToken`

### Obsługa błędów

- Globalny handler wyjątków (`IExceptionHandler` lub middleware)
- Odpowiedzi błędów w formacie RFC 7807 Problem Details
- Logowanie przez `ILogger<T>` ze structured logging — nigdy `Console.WriteLine`
- Błędy callbacku do zewnętrznego systemu logować i zwracać odpowiedni status — nie przerywać wątku

### Bezpieczeństwo

- Endpoint `/approvals` chroniony kluczem API w nagłówku (`X-Api-Key`)
- Endpoint `/api/messages` weryfikowany przez Bot Framework (JWT z Azure)
- Nigdy nie logować pełnych payloadów zawierających dane biznesowe na poziomie INFO
- Wszystkie zewnętrzne wywołania HTTP mają skonfigurowany timeout

---

## Testing Guidelines

- **xUnit** jako framework testowy
- **Moq** do mockowania zależności (`ITeamsNotifier`, `IApprovalService`, `HttpClient`)
- **FluentAssertions** do czytelnych asercji
- Nazewnictwo testów: `MethodName_Scenario_ExpectedResult`
  - Przykład: `HandleDecision_ValidApprove_CallsCallbackWithCorrectPayload`
- Testy integracyjne: `WebApplicationFactory<Program>` z podmienionymi serwisami Teams
- Nie mockować Bot Framework internals — testować serwisy w izolacji

---

## AI Assistant Instructions

1. **Czytaj przed modyfikacją** — zawsze przeczytaj plik przed edycją
2. **Cienkie kontrolery** — logika należy do serwisów, nie do kontrolerów
3. **Zawsze async/await** — nigdy `.Result` ani `.Wait()`
4. **Brak sekretów w kodzie** — konfiguracja przez Secret Manager lub env vars
5. **Waliduj wejście** — wszystkie DTO z zewnątrz muszą być walidowane (Data Annotations lub FluentValidation)
6. **Używaj `ILogger<T>`** — nie `Console.WriteLine`
7. **CancellationToken** — przekazuj go przez cały stos async
8. **Uruchom testy** — `dotnet test` musi przejść przed commitem
9. **Minimalne zmiany** — nie refaktoruj kodu niezwiązanego z zadaniem
10. **Aktualizuj CLAUDE.md** — gdy zmienia się architektura lub konwencje

---

## Useful References

- [Bot Framework SDK dla C#](https://learn.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-sdk-quickstart)
- [Proaktywne wiadomości w Teams](https://learn.microsoft.com/en-us/microsoftteams/platform/bots/how-to/conversations/send-proactive-messages)
- [Adaptive Cards](https://adaptivecards.io/)
- [Adaptive Card Designer](https://adaptivecards.io/designer/)
- [Microsoft Graph SDK (.NET)](https://learn.microsoft.com/en-us/graph/sdks/sdks-overview)
- [Microsoft Entra ID — rejestracja aplikacji](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
- [ASP.NET Core Problem Details](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling#problem-details)
- [Conventional Commits](https://www.conventionalcommits.org/)

---

*Last updated: 2026-03-20 — Teams approval bot architecture defined*
