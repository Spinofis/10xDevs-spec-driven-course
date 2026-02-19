# API Endpoint Implementation Plan: GET /me/profile

## 1. Przegląd punktu końcowego
Endpoint zwraca **profil preferencji** aktualnie zalogowanego użytkownika oraz listę jego **tagów preferencji** (z kolejnością). Jest to endpoint typu *read/query* i wymaga uwierzytelnienia (JWT Bearer).

## 2. Szczegóły żądania
- Metoda HTTP: `GET`
- Struktura URL: `/me/profile`
- Nagłówki:
  - Wymagane: `Authorization: Bearer <token>`
  - Opcjonalne: `X-Correlation-Id` (jeśli brak, generowany i zwracany w odpowiedzi)
- Parametry:
  - Wymagane: brak
  - Opcjonalne: brak
- Request Body: brak
- Wykorzystywane typy (Application/Features/Me/Queries):
  - `GetUserProfileQueryRequest` / `GetUserProfileQuery` / `GetUserProfileQueryResponse`
  - `UserProfileQueryModel`, `PreferenceTagQueryModel`
  - `TagQueryModel` (Application/Features/Tags/Queries/Models)

## 3. Szczegóły odpowiedzi
- `200 OK`:
  ```json
  {
    "userId": "uuid",
    "profile": {
      "defaultBudgetLevel": "low|medium|high|null",
      "defaultPeopleCount": 2,
      "defaultPace": "relaxed|normal|fast|null",
      "defaultNotes": "string|null",
      "isDefault": true,
      "createdAt": "timestamp",
      "updatedAt": "timestamp"
    },
    "preferenceTags": [
      {
        "tag": { "id": "uuid", "code": "mountains", "displayName": "Mountains" },
        "order": 1,
        "createdAt": "timestamp"
      }
    ]
  }
  ```
- Nagłówki odpowiedzi:
  - `X-Correlation-Id` (przekazany lub wygenerowany)
- Kody błędów (ProblemDetails + stabilne kody):
  - `401` (np. `UNAUTHORIZED`) – brak/niepoprawny JWT
  - `404` (np. `USER_NOT_FOUND`) – poprawny JWT, ale użytkownik nie istnieje w DB (nieprawidłowy stan)
  - `500 INTERNAL_ERROR` – nieoczekiwane błędy serwera

> Uwaga dot. kontraktu enumów: specyfikacja wymaga wartości tekstowych (`low|medium|high`, `relaxed|normal|fast`). Minimal API domyślnie serializuje enumy jako liczby, więc należy skonfigurować `JsonStringEnumConverter` z `JsonNamingPolicy.CamelCase`.

## 4. Przepływ danych
1. Klient wywołuje `GET /me/profile` z `Authorization: Bearer <token>` i opcjonalnym `X-Correlation-Id`.
2. Minimal API (np. `MeEndpoints`) odczytuje/generuje `X-Correlation-Id` i ustawia go w odpowiedzi.
3. Endpoint wyciąga `userId` z kontekstu autoryzacji (JWT claim, np. `sub`/`nameidentifier`).
4. Endpoint tworzy `GetUserProfileQuery` i wysyła go przez `IMediator`.
5. `GetUserProfileQueryHandler`:
   - pobiera profil z tabeli `user_profile` (1:1 dla `user_id`),
   - pobiera tagi preferencji użytkownika z `user_preference_tag` oraz dołącza dane słownikowe z `tag`,
   - mapuje wynik do `UserProfileQueryModel` oraz listy `PreferenceTagQueryModel` (posortowane po `order`).
6. Handler zwraca `GetUserProfileQueryResponse`, a endpoint zwraca `200 OK` z payloadem JSON.

## 5. Względy bezpieczeństwa
- Endpoint musi być **chroniony JWT** (`RequireAuthorization()`); nie może działać dla niezalogowanych.
- `userId` musi pochodzić wyłącznie z tokenu (brak ID w URL/query), co eliminuje typowe IDOR-y.
- W handlerze filtruj dane po `userId` (ownership) niezależnie od ewentualnych polityk DB/RLS.
- Nie loguj danych wrażliwych (tokenów, pełnych obiektów profilu, itp.); loguj jedynie `userId`, `traceId` i `correlationId`.
- Zwracaj `ProblemDetails` bez ujawniania szczegółów infrastruktury (np. wyjątków DB).

## 6. Obsługa błędów
- `401 Unauthorized`:
  - brak nagłówka `Authorization`,
  - token wygasł / ma zły podpis / nie przeszedł walidacji.
- `404 USER_NOT_FOUND` (opcjonalnie, zależnie od decyzji produktowej):
  - token poprawny, ale użytkownik nie istnieje w DB (np. usunięty lub dane niespójne).
- `500 INTERNAL_ERROR`:
  - błędy DB, mapowania lub inne nieobsłużone wyjątki.
- Mapowanie do `ProblemDetails` realizuje `ExceptionHandlingMiddleware`.
- Brak dedykowanej tabeli błędów w aktualnym schemacie: użyj `ILogger` + `traceId` w `ProblemDetails`.

## 7. Wydajność
- Używaj `AsNoTracking()` dla odczytów.
- Preferuj 1–2 zapytania do DB (profil + tagi), albo jedno zapytanie z joinami, jeśli czytelne.
- Zapewnij indeksy wspierające odczyt:
  - dla `user_profile`: PK/UNIQUE na `user_id`,
  - dla `user_preference_tag`: indeks po `(user_id, "order", tag_id)` + FK do `tag`.
- Dane mają niewielki wolumen (pojedynczy profil + lista tagów), więc koszt endpointu jest niski; nie wymagaj cache w MVP.

## 8. Kroki implementacji
1. **Uwierzytelnianie i dostęp do `userId`**
   - Skonfiguruj `AddAuthentication().AddJwtBearer(...)` + `app.UseAuthentication()` przed `UseAuthorization()` (jeśli nie istnieje jeszcze w projekcie).
   - Ustal standardowy claim zawierający `userId` (np. `sub`) i wspólną metodę ekstrakcji (np. extension na `ClaimsPrincipal` w warstwie API).
2. **Konfiguracja JSON dla enumów**
   - Dodaj konfigurację `HttpJsonOptions` (Minimal API) z `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`, aby zwracać `low/medium/high` i `relaxed/normal/fast` zamiast wartości liczbowych.
3. **Modele domenowe i EF Core (jeśli brak)**
   - Dodaj encje:
     - `UserProfile` (1:1 z użytkownikiem),
     - `UserPreferenceTag` (N:M użytkownik–tag, z polem `Order`).
   - Dodaj konfiguracje EF + `DbSet<>` w `AppDbContext` i `IAppDbContext`.
   - Uzgodnij nazwy tabel/kolumn z `.ai/db_plan.md` vs istniejącym mapowaniem (`users`, `tags`) i przygotuj migracje.
4. **Warstwa aplikacyjna – query**
   - Zaktualizuj `GetUserProfileQuery` tak, aby implementował `IRequest<GetUserProfileQueryResponse>`.
   - Doprecyzuj źródło `userId` dla handlera:
     - rekomendacja: dodaj `Guid UserId` do `GetUserProfileQueryRequest` i mapuj go w endpointzie z JWT,
     - alternatywnie: wprowadź `ICurrentUserContext` w `VibeTravels.Application.Abstractions` i wstrzykuj do handlera.
5. **Handler MediatR**
   - Dodaj `GetUserProfileQueryHandler` (np. `VibeTravels.Application/Features/Me/Handlers`):
     - pobranie profilu użytkownika,
     - pobranie tagów preferencji wraz z danymi `Tag`,
     - sortowanie po `Order`,
     - mapowanie do `UserProfileQueryModel` / `PreferenceTagQueryModel`.
   - Ustal zachowanie, gdy profil nie istnieje:
     - rekomendacja: twórz profil domyślny przy rejestracji; w query zakładaj istnienie,
     - w razie braku: zwróć profil domyślny (bez zapisu) albo potraktuj to jako błąd stanu (`500`/`404`) – decyzja musi być spójna w całym API.
6. **Endpoint minimal API**
   - Dodaj `MeEndpoints` i mapowanie:
     - `var group = app.MapGroup("/me").WithTags("Me");`
     - `group.MapGet("/profile", GetProfile).RequireAuthorization();`
     - `.Produces<GetUserProfileQueryResponse>(200)` + `.ProducesProblem(401)` + `.ProducesProblem(404)`.
   - Ustawianie `X-Correlation-Id` analogicznie do `AuthEndpoints` i `TagsEndpoints`.
7. **Testy**
   - Integracyjne:
     - `GET /me/profile` bez JWT → `401`,
     - z JWT → `200` + poprawne mapowanie profilu i tagów (kolejność po `order`),
     - weryfikacja formatu enumów (string, camelCase),
     - obecność `X-Correlation-Id`.
   - Jeśli brak gotowego mechanizmu auth w testach: dodaj testowy schemat uwierzytelniania w `ApiFactory` (np. `TestAuthHandler`) umożliwiający ustawienie `userId` w claims.

