# API Endpoint Implementation Plan: POST /trips

## 1. Przegląd punktu końcowego
Endpoint tworzy nową wycieczkę (`trip`) wraz z opcjonalnymi tagami wycieczki (`trip_tag`). Jest to endpoint typu *command/write* i wymaga uwierzytelnienia (JWT Bearer). Po utworzeniu zasobu zwraca `201 Created` z pełnym DTO wycieczki oraz listą dołączonych tagów.
Na razie pomin autoryzacje!!!

## 2. Szczegóły żądania
- Metoda HTTP: `POST`
- Struktura URL: `/trips`
- Nagłówki:
  - Wymagane: `Authorization: Bearer <token>`  -> Na razie pomin
  - Opcjonalne: `X-Correlation-Id` (jeśli brak, generowany i zwracany w odpowiedzi)
- Parametry:
  - Wymagane: brak (wszystko w body)
  - Opcjonalne: brak
- Request Body (JSON):
  - Wymagane:
    - `title` (string, niepusty)
    - `placeText` (string, niepusty)
  - Opcjonalne:
    - `noteText` (string|null)
    - `dateFrom` (YYYY-MM-DD|null)
    - `dateTo` (YYYY-MM-DD|null) — jeśli podane obie daty: `dateTo >= dateFrom`
    - `stayLengthMinDays` (int|null) — jeśli podane: `> 0`
    - `stayLengthMaxDays` (int|null) — jeśli podane: `> 0`
    - `peopleCount` (int|null) — jeśli podane: `> 0`
    - `budgetLevel` (`low|medium|high|null`)
    - `pace` (`relaxed|normal|fast|null`)
    - `tags` (array|null):
      - element: `{ "tagId": "uuid", "order": 1 }`
      - `tagId` wymagane, `order` opcjonalne (rekomendacja: traktować `null` jako `0`)
- Wykorzystywane typy (Application/Features/Trips/Commands + Queries/Models):
  - `CreateTripCommandModel`, `TripTagCommandModel`
  - `CreateTripCommandRequest`, `CreateTripCommand`, `CreateTripCommandResponse`
  - `TripQueryModel`, `TripTagQueryModel`
  - `TagQueryModel` (Application/Features/Tags/Queries/Models)
  - Enumy: `BudgetLevel`, `Pace` (Application/Features/Common/Enums.cs)

## 3. Szczegóły odpowiedzi
- `201 Created`:
  ```json
  {
    "trip": {
      "id": "uuid",
      "userId": "uuid",
      "title": "Trip to Rome",
      "placeText": "Rome, Italy",
      "noteText": "We love food and history",
      "dateFrom": "2026-05-01",
      "dateTo": "2026-05-07",
      "stayLengthMinDays": 5,
      "stayLengthMaxDays": 7,
      "peopleCount": 2,
      "budgetLevel": "medium",
      "pace": "normal",
      "generatedAt": null,
      "hasGeneratedPlan": false,
      "createdAt": "timestamp",
      "updatedAt": "timestamp"
    },
    "tags": [
      {
        "tag": { "id": "uuid", "code": "museums", "displayName": "Museums", "createdAt": "timestamp" },
        "order": 1,
        "createdAt": "timestamp"
      }
    ]
  }
  ```
- Nagłówki odpowiedzi:
  - `X-Correlation-Id` (przekazany lub wygenerowany)
- Kody błędów (ProblemDetails + stabilne kody):
  - `400 VALIDATION_ERROR` – nieprawidłowe dane wejściowe (FluentValidation)
  - `401 UNAUTHORIZED` – brak/niepoprawny JWT
  - `404 TAG_NOT_FOUND` – przekazano tagId, który nie istnieje (lub nie jest dostępny, jeśli tagi nie są publiczne)
  - `500 INTERNAL_ERROR` – nieoczekiwane błędy serwera

> Uwaga dot. kontraktu enumów: specyfikacja wymaga wartości tekstowych (`low|medium|high`, `relaxed|normal|fast`). Minimal API domyślnie serializuje enumy jako liczby, więc należy skonfigurować `JsonStringEnumConverter` z `JsonNamingPolicy.CamelCase`.

## 4. Przepływ danych
1. Klient wywołuje `POST /trips` z `Authorization: Bearer <token>` i opcjonalnym `X-Correlation-Id`.
2. Minimal API (`TripsEndpoints`) odczytuje/generuje `X-Correlation-Id` i ustawia go w odpowiedzi.
3. Endpoint wyciąga `userId` z kontekstu autoryzacji (JWT claim, np. `sub`/`nameidentifier`).
4. Endpoint mapuje payload do `CreateTripCommandRequest` (z `CreateTripCommandModel`) i wysyła `CreateTripCommand` przez `IMediator`.
5. `CreateTripCommandHandler`:
   - waliduje wymagania biznesowe (poza FluentValidation tylko, jeśli potrzebne),
   - w transakcji:
     - tworzy rekord `trip` z `userId` (ownership),
     - jeśli `tags` podane:
       - weryfikuje istnienie wszystkich `tagId` (jeden SELECT po IN),
       - tworzy rekordy `trip_tag` z zachowaniem `order` (domyślnie `0`) i spójnością unikalności `(trip_id, tag_id)`.
   - mapuje wynik do `TripQueryModel` + listy `TripTagQueryModel` (posortowane po `order` rosnąco).
6. Endpoint zwraca `201 Created` z JSON-em `{ trip, tags }`.

## 5. Względy bezpieczeństwa
- Endpoint musi być chroniony JWT (`RequireAuthorization()`).
- `userId` musi pochodzić wyłącznie z tokenu (nigdy z body/query), aby uniknąć IDOR i fałszywego przypisania ownership.
- W handlerze zapisuj zawsze `trip.user_id = userId` z kontekstu, niezależnie od danych wejściowych.
- Waliduj rozmiary wejścia (limity długości dla `title/placeText/noteText`), aby ograniczyć ryzyko nadużyć (DoS poprzez duże payloady).
- Nie loguj treści `noteText` (potencjalnie wrażliwe dane); loguj jedynie `tripId`, `userId`, `traceId`, `correlationId`.

## 6. Obsługa błędów
- `400 VALIDATION_ERROR`:
  - `title` pusty/null,
  - `placeText` pusty/null,
  - jeśli obie daty podane: `dateTo < dateFrom`,
  - `peopleCount <= 0`,
  - `stayLengthMinDays <= 0` / `stayLengthMaxDays <= 0`,
  - niepoprawny format daty/UUID (deserializacja) → traktuj jako `400`.
- `404 TAG_NOT_FOUND`:
  - jeśli `tags` zawiera `tagId`, którego nie ma w tabeli `tag`.
  - rekomendacja: w `details` zwrócić listę brakujących `tagId` (bez ujawniania danych wrażliwych).
- `401 Unauthorized`:
  - brak nagłówka `Authorization`,
  - token wygasł / ma zły podpis / nie przeszedł walidacji.
- `500 INTERNAL_ERROR`:
  - błędy DB (np. constrainty), transakcji, mapowania lub inne nieobsłużone wyjątki.

Mapowanie do `ProblemDetails` realizuje `ExceptionHandlingMiddleware`. Brak dedykowanej tabeli błędów w aktualnym planie DB: użyj `ILogger` + `traceId` w `ProblemDetails`. (Jeśli w przyszłości pojawi się tabela błędów/auditów, można dodać event `trip_created` w `audit_event` – opcjonalne.)

## 7. Wydajność
- Weryfikacja tagów: jeden SELECT `WHERE id IN (...)` + porównanie liczności, zamiast N zapytań.
- Operacje zapisu w jednej transakcji (trip + trip_tag), aby uniknąć pół-zapisów.
- Rozważ limity: max liczba tagów na trip (np. 20) i max długość tekstów (tytuł/miejsce/notatka).
- Indeksy (wg `.ai/db_plan.md`):
  - `trip_user_created_idx (user_id, created_at DESC)`
  - `trip_tag_trip_order_idx (trip_id, "order", tag_id)`
  - `trip_tag_tag_idx (tag_id)`

## 8. Kroki implementacji
1. **Ustalenie kontraktu i spójności schematu**
   - Zweryfikuj spójność pomiędzy `.ai/db_plan.md` (np. `app_user`, `tag`) a aktualnym mapowaniem EF (np. `users`, `tags`, `label`).
   - Wybierz jedno źródło prawdy i dostosuj mapowania/migracje tak, by endpoint `POST /trips` działał na docelowym schemacie.
2. **Uwierzytelnianie i dostęp do `userId`**
   - Skonfiguruj `AddAuthentication().AddJwtBearer(...)` + `app.UseAuthentication()` przed `UseAuthorization()` (jeśli nie istnieje).
   - Dodaj wspólną metodę ekstrakcji `userId` z `ClaimsPrincipal` (np. extension w warstwie API).
3. **Konfiguracja JSON (enumy i daty)**
   - Dodaj konfigurację `HttpJsonOptions` z `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`.
   - Zweryfikuj serializację `DateOnly` jako `"YYYY-MM-DD"` (w razie potrzeby dodaj konwerter dla `DateOnly`).
4. **Warstwa danych: encje i EF Core**
   - Dodaj encje domenowe + konfiguracje EF:
     - `Trip` (tabela `trip`) z polami zgodnymi ze specyfikacją,
     - `TripTag` (tabela `trip_tag`) z `(trip_id, tag_id)` + `Order`.
   - Zaktualizuj `IAppDbContext` oraz `AppDbContext` o `DbSet<Trip>` i `DbSet<TripTag>`.
   - Dodaj migracje tworzące tabele/constrainty/indeksy.
5. **Warstwa aplikacyjna: Command + Validator**
   - Upewnij się, że `CreateTripCommand` implementuje `IRequest<CreateTripCommandResponse>`.
   - Dodaj `CreateTripCommandRequestValidator` (FluentValidation) walidujący `request.Model.*`:
     - `Title`, `PlaceText` wymagane,
     - relacje dat, wartości dodatnie,
     - limit liczby tagów i dopuszczalne `Order` (np. `>= 0`).
   - Dodaj dedykowany wyjątek `TagNotFoundException : AppException` (`404`, kod `TAG_NOT_FOUND`) albo wspólny `NotFoundException` z kodami per zasób.
6. **Handler MediatR**
   - Dodaj `CreateTripCommandHandler`:
     - pobranie `userId` (rekomendacja: do `CreateTripCommandRequest` dodaj `Guid UserId` i uzupełniaj go w endpointzie; alternatywnie: `ICurrentUserContext` w Application.Abstractions),
     - transakcyjny zapis `trip` + `trip_tag`,
     - walidacja istnienia tagów,
     - mapowanie do `TripQueryModel` i `TripTagQueryModel`.
   - Wyodrębnij logikę, jeśli zacznie puchnąć:
     - `ITripCreationService` (aplikacyjny) dla: walidacji tagów, budowy encji, transakcji,
     - lub mały helper do mapowania `Trip` → `TripQueryModel` (bez logiki biznesowej).
7. **Endpoint minimal API**
   - Dodaj `VibeTravelers.API/Endpoints/TripsEndpoints.cs`:
     - `var group = app.MapGroup("/trips").WithTags("Trips");`
     - `group.MapPost("/", CreateTrip).RequireAuthorization();`
     - `.Produces<CreateTripCommandResponse>(201)` + `.ProducesProblem(400)` + `.ProducesProblem(401)` + `.ProducesProblem(404)`.
   - Ustawianie `X-Correlation-Id` analogicznie do `AuthEndpoints` i `TagsEndpoints`.
   - Dodaj `app.MapTripsEndpoints();` w `VibeTravelers.API/Program.cs`.
8. **Testy**
   - Integracyjne:
     - `POST /trips` bez JWT → `401`,
     - poprawny request → `201` + zwracany `trip.id`, `userId` z JWT, `hasGeneratedPlan=false`,
     - z tagami: poprawne sortowanie po `order`, `404 TAG_NOT_FOUND` dla brakujących tagów,
     - `400` dla `dateTo < dateFrom`, `peopleCount <= 0`, brak `title/placeText`,
     - enumy jako string (camelCase) i obecność `X-Correlation-Id`.

