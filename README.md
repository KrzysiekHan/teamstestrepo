# Teams Approval Bot

ASP.NET Core 8 Web API zastępujący ręczny proces zatwierdzania przez Microsoft Teams. Zewnętrzny system inicjuje zatwierdzenie — pracownik otrzymuje formularz w Teams, wypełnia go i klika **Zatwierdź** lub **Odrzuć**, a decyzja wraca automatycznie do systemu źródłowego.

---

## Spis treści

- [Jak to działa](#jak-to-działa)
- [Wymagania](#wymagania)
- [Konfiguracja](#konfiguracja)
- [Uruchomienie](#uruchomienie)
- [API](#api)
- [Testowanie](#testowanie)
- [Struktura projektu](#struktura-projektu)
- [Wdrożenie](#wdrożenie)

---

## Jak to działa

```
Zewnętrzny system
      │
      │  POST /approvals  { userId, processId, callbackUrl, ... }
      │  X-Api-Key: <secret>
      ▼
┌──────────────────────────────────┐
│       Teams Approval Bot API     │
│                                  │
│  ApprovalsController             │
│       └─ ApprovalService         │
│              └─ TeamsNotifier    │──► Microsoft Graph / Bot Framework
└──────────────────────────────────┘          │
                                              │  Adaptive Card
                                              ▼
                                       Pracownik w Teams
                                              │
                                              │  Wypełnia formularz + klika przycisk
                                              ▼
                                         ApprovalBot
                                              │
      ┌───────────────────────────────────────┘
      │  POST {callbackUrl}  { decision, field1, field2, ... }
      ▼
Zewnętrzny system
```

**Przepływ krok po kroku:**

1. Zewnętrzny system wywołuje `POST /approvals` z danymi procesu i identyfikatorem pracownika.
2. Aplikacja pobiera tożsamość użytkownika z Microsoft Entra ID (Azure AD) przez Graph API.
3. Do pracownika trafia wiadomość w Teams — Adaptive Card z dwoma polami i przyciskami **Zatwierdź** / **Odrzuć**.
4. Pracownik wypełnia formularz i klika przycisk.
5. Bot Framework dostarcza odpowiedź do endpointu `/api/messages`.
6. Aplikacja wywołuje `callbackUrl` z decyzją i danymi z formularza.

---

## Wymagania

| Narzędzie | Wersja |
|---|---|
| .NET SDK | 8.0+ |
| Azure subscription | — |
| Microsoft Entra ID (Azure AD) | — |
| Azure Bot Service | — |

---

## Konfiguracja

### 1. Rejestracja aplikacji w Entra ID

1. W [portalu Azure](https://portal.azure.com) utwórz **App Registration**.
2. Zanotuj **Application (client) ID** oraz **Directory (tenant) ID**.
3. Utwórz **Client Secret** (Certificates & secrets → New client secret).
4. Przyznaj uprawnienia Microsoft Graph:
   - `User.Read.All` (Application) — wyszukiwanie użytkowników po UPN

### 2. Azure Bot Service

1. Utwórz zasób **Azure Bot**.
2. Podaj Client ID z kroku 1.
3. Po uruchomieniu aplikacji ustaw **Messaging Endpoint**: `https://<twoja-domena>/api/messages`.
4. Włącz kanał **Microsoft Teams**.

### 3. Sekrety aplikacji (lokalnie)

```bash
cd teamstestrepo

dotnet user-secrets init --project src/TeamstestRepo.Api

dotnet user-secrets set "BotFramework:AppSecret"    "<client-secret>"       --project src/TeamstestRepo.Api
dotnet user-secrets set "ApiKey:InboundSecret"      "<twój-klucz-api>"      --project src/TeamstestRepo.Api
dotnet user-secrets set "Graph:ClientSecret"        "<client-secret>"       --project src/TeamstestRepo.Api
```

### 4. appsettings.json (niesekrety)

```json
{
  "BotFramework": {
    "AppId":    "<Application (client) ID>",
    "TenantId": "<Directory (tenant) ID>"
  },
  "ExternalApi": {
    "TimeoutSeconds": 30
  }
}
```

> **Uwaga:** Nigdy nie umieszczaj sekretów bezpośrednio w `appsettings.json` ani w kodzie.

---

## Uruchomienie

```bash
# Przywróć zależności
dotnet restore

# Uruchom API (domyślnie http://localhost:5000)
dotnet run --project src/TeamstestRepo.Api

# Hot reload podczas developmentu
dotnet watch run --project src/TeamstestRepo.Api
```

### Lokalny tunel dla Bot Framework

Bot Framework musi mieć dostęp do publicznego URL endpointu `/api/messages`. Lokalnie użyj tunelu:

```bash
# ngrok
ngrok http 5000
# → skopiuj https://<hash>.ngrok.io

# lub devtunnel (Microsoft)
devtunnel host -p 5000
```

Następnie w Azure Bot zaktualizuj **Messaging Endpoint**: `https://<tunnel-url>/api/messages`.

---

## API

### `POST /approvals`

Inicjuje proces zatwierdzania. Wysyła Adaptive Card do wskazanego pracownika w Teams.

**Nagłówki:**

| Nagłówek | Opis |
|---|---|
| `X-Api-Key` | Klucz API (`ApiKey:InboundSecret`) |
| `Content-Type` | `application/json` |

**Ciało żądania:**

```json
{
  "processId":    "zamowienie-2026-001",
  "userId":       "jan.kowalski@firma.pl",
  "callbackUrl":  "https://erp.firma.pl/api/approvals/callback",
  "field1Label":  "Komentarz",
  "field2Label":  "Numer zlecenia",
  "processTitle": "Zatwierdzenie zamówienia #2026-001"
}
```

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `processId` | `string` | tak | Unikalny ID procesu w zewnętrznym systemie |
| `userId` | `string` | tak | UPN (`user@domain.com`) lub Object ID z Entra ID |
| `callbackUrl` | `string` (URL) | tak | Adres, pod który wróci decyzja |
| `field1Label` | `string` (max 100) | tak | Etykieta pierwszego pola formularza |
| `field2Label` | `string` (max 100) | tak | Etykieta drugiego pola formularza |
| `processTitle` | `string` (max 200) | nie | Tytuł wyświetlany na karcie |

**Odpowiedź `202 Accepted`:**

```json
{
  "processId": "zamowienie-2026-001",
  "message":   "Approval card sent to user jan.kowalski@firma.pl."
}
```

**Błędy:**

| Kod | Opis |
|---|---|
| `400` | Nieprawidłowe lub niekompletne ciało żądania |
| `401` | Brak nagłówka `X-Api-Key` |
| `403` | Nieprawidłowy klucz API |

---

### Callback (wychodzący)

Po decyzji pracownika aplikacja wywołuje `POST {callbackUrl}`:

```json
{
  "processId":  "zamowienie-2026-001",
  "decision":   "approve",
  "decidedBy":  "aad-object-id-pracownika",
  "decidedAt":  "2026-03-20T15:00:00Z",
  "field1":     "Zgadzam się z zamówieniem.",
  "field2":     "ZAM/2026/001"
}
```

| Pole | Wartości |
|---|---|
| `decision` | `approve` \| `reject` |

---

## Testowanie

```bash
# Wszystkie testy
dotnet test

# Tylko unit testy
dotnet test tests/TeamstestRepo.UnitTests

# Z logowaniem
dotnet test --logger "console;verbosity=detailed"
```

### Ręczne wywołanie (curl)

```bash
curl -X POST http://localhost:5000/approvals \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: <twój-klucz-api>" \
  -d '{
    "processId":   "test-001",
    "userId":      "jan.kowalski@firma.pl",
    "callbackUrl": "https://webhook.site/<twój-id>",
    "field1Label": "Komentarz",
    "field2Label": "Numer zlecenia"
  }'
```

> Użyj [webhook.site](https://webhook.site) do podglądu payloadu callback.

---

## Struktura projektu

```
teamstestrepo/
├── CLAUDE.md                                  # Instrukcje dla asystentów AI
├── README.md
├── teamstestrepo.sln
├── src/
│   └── TeamstestRepo.Api/
│       ├── Controllers/
│       │   ├── ApprovalsController.cs          # POST /approvals
│       │   └── BotController.cs                # POST /api/messages
│       ├── Models/
│       │   ├── ApprovalRequest.cs              # DTO wejściowy
│       │   ├── ApprovalDecision.cs             # DTO wysyłany do callbackUrl
│       │   ├── AdaptiveCardPayload.cs          # DTO z formularza Teams
│       │   └── ApprovalCreatedResponse.cs      # DTO odpowiedzi 202
│       ├── Services/
│       │   ├── IApprovalService.cs
│       │   ├── ApprovalService.cs              # Logika biznesowa
│       │   ├── ITeamsNotifier.cs
│       │   ├── TeamsNotifier.cs                # Wysyłanie kart do Teams
│       │   └── ApprovalBot.cs                  # Obsługa odpowiedzi z kart
│       ├── Cards/
│       │   └── ApprovalCard.json               # Szablon Adaptive Card
│       ├── Middleware/
│       │   └── ApiKeyMiddleware.cs             # Weryfikacja X-Api-Key
│       ├── appsettings.json
│       └── Program.cs
└── tests/
    ├── TeamstestRepo.UnitTests/
    │   └── ApprovalServiceTests.cs             # 4 scenariusze unit
    └── TeamstestRepo.IntegrationTests/
        └── ApprovalsEndpointTests.cs           # 4 scenariusze e2e
```

---

## Wdrożenie

### Zmienne środowiskowe (produkcja)

Na serwerze / w Azure App Service ustaw:

| Zmienna środowiskowa | Opis |
|---|---|
| `BotFramework__AppId` | Application (client) ID |
| `BotFramework__TenantId` | Directory (tenant) ID |
| `BotFramework__AppSecret` | Client Secret bota |
| `ApiKey__InboundSecret` | Klucz API dla `/approvals` |

### Build produkcyjny

```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### Ważne uwagi

- **Callback registry** jest przechowywany w pamięci (`ConcurrentDictionary`). Przed wdrożeniem produkcyjnym zastąp go trwałym storage (np. Azure Table Storage, Redis lub SQL).
- Upewnij się, że `ServiceUrl` w `TeamsNotifier.cs` odpowiada regionowi Twojego tenanta Teams (EMEA, Americas, APAC).
- Endpoint `/api/messages` jest zabezpieczony przez Bot Framework (JWT z Azure) — nie wymaga dodatkowej konfiguracji.

---

## Przydatne linki

- [Bot Framework SDK dla C#](https://learn.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-sdk-quickstart)
- [Proaktywne wiadomości w Teams](https://learn.microsoft.com/en-us/microsoftteams/platform/bots/how-to/conversations/send-proactive-messages)
- [Adaptive Cards Designer](https://adaptivecards.io/designer/)
- [Microsoft Graph SDK (.NET)](https://learn.microsoft.com/en-us/graph/sdks/sdks-overview)
- [ASP.NET Core Problem Details](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling#problem-details)
