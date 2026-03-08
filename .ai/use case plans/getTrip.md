# API Endpoint Implementation Plan: GET /trips

## 1. Przeglad punktu koncowego
Endpoint sluzy do listowania wycieczek (`trip`) z filtrowaniem, sortowaniem i paginacja kursorem. Jest to endpoint typu *query/read*.

Zgodnie ze specyfikacja:
- filtr `q` przeszukuje `title`, `placeText`, `noteText`,
- filtr `hasPlan` mapuje sie na `hasGeneratedPlan`,
- paginacja: `limit` + `cursor`,
- sortowanie: `createdAt`, `generatedAt`, `title` z opcjonalnym prefiksem `-` dla malejaco.

UWAGA!!!
Mimo ze jest tu wymieniona autoryzacja to na razie ja pomin w implementacji!!!!

## 2. Szczegoly zadania
- Metoda HTTP: `GET`
- Struktura URL: `/trips`
- Naglowki:
  - Opcjonalne: `X-Correlation-Id` (jesli brak, generowany i zwracany w odpowiedzi)
  - Docelowo (wg `.cursor/rules/shared.mdc`): `Authorization: Bearer <token>` dla endpointu prywatnego
- Parametry (query):
  - Opcjonalne:
    - `q` (`string?`): wyszukiwanie w `title`, `placeText`, `noteText`,
    - `hasPlan` (`bool?`): `true|false` (mapuje na `HasGeneratedPlan`)
    - `includeDeleted` (`bool?`): `true|false`, domyslnie `false`
    - `limit` (`int?`): domyslnie `20`, zakres `1..100`
    - `cursor` (`string?`): nieprzezroczysty cursor do paginacji (wymagany format aplikacyjny)
    - `sort` (`string?`): dozwolone: `createdAt`, `generatedAt`, `title` oraz warianty malejace `-createdAt`, `-generatedAt`, `-title`
- Request Body: brak

Uwagi o mapowaniu nazw:
- w typie `ListTripsQueryRequest` pole na wyszukiwanie nazywa sie `Query`, natomiast parametr HTTP to `q`; w endpointzie Minimal API zmapuj jawnie `q -> Query`.

## 3. Szczegoly odpowiedzi
- `200 OK`:
  ```json
  {
    "items": [
      {
        "id": "uuid",
        "userId": "uuid",
        "title": "string",
        "placeText": "string|null",
        "noteText": "string|null",
        "dateFrom": "YYYY-MM-DD|null",
        "dateTo": "YYYY-MM-DD|null",
        "stayLengthMinDays": 2,
        "stayLengthMaxDays": 7,
        "peopleCount": 2,
        "budgetLevel": "low|medium|high|null",
        "pace": "relaxed|normal|fast|null",
        "generatedAt": "timestamp|null",
        "hasGeneratedPlan": false,
        "createdAt": "timestamp",
        "updatedAt": "timestamp"
      }
    ],
    "nextCursor": "string|null"
  }
  ```
- Naglowki odpowiedzi:
  - `X-Correlation-Id` (przekazany lub wygenerowany)
- Bledy:
  - `400 VALIDATION_ERROR` (nieprawidlowy `sort`, zly format `cursor`, `limit` poza zakresem)
  - `401 UNAUTHORIZED` (docelowo, gdy endpoint bedzie chroniony JWT)
  - `500 INTERNAL_ERROR` (nieoczekiwane bledy serwera)

## 4. Wykorzystywane typy
Warstwa Application (zgodnie z regula lokalizacji w `.cursor/rules/backend.mdc`):
- `VibeTravels.Application.Features.Trips.Queries.ListTripsQueryRequest` (juz istnieje)
- `VibeTravels.Application.Features.Trips.Queries.ListTripsQuery`
  - rekomendacja: `IRequest<Result<ListTripsQueryResponse>>`, bo endpoint moze zwracac `400 VALIDATION_ERROR`
- `VibeTravels.Application.Features.Trips.Queries.ListTripsQueryResponse`
  - wymagane przez kontrakt: `Items` + `NextCursor`
  - obecnie `PagedResponse<T>` nie ma `NextCursor` (wymaga doprecyzowania/rozszerzenia)
- `VibeTravels.Application.Features.Trips.Queries.Models.TripQueryModel` (juz istnieje)

Warstwa API:
- `VibeTravelers.API.Endpoints.TripsEndpoints` (dodanie `MapGet("/", ListTrips)` i mapowanie query params)
- `VibeTravelers.API.ResultHttpMapper` (mapowanie `Result` -> `ProblemDetails` z `errors[]` oraz `traceId` i `correlationId`)

Warstwa infrastruktury/DB:
- EF Core: zapytanie po `IAppDbContext.Trips` (AsNoTracking, projekcja do `TripQueryModel`)
- kolumny do filtrowania/sortowania/paginacji: `created_at`, `generated_at`, `title`, `place_text`, `has_generated_plan`
- soft delete (dla `includeDeleted`):
  - kontrakt oczekuje filtra po `deleted_at` (wg `.ai/db_plan.md`)
  - obecny model domeny/EF nie ma `DeletedAt`; trzeba zdecydowac: dodac `DeletedAt` do encji + konfiguracji EF + migracji albo jawnie odlozyc `includeDeleted` (odlozenie = niespojnosc ze specyfikacja)

## 5. Przeplyw danych
1. Klient wywoluje `GET /trips` z opcjonalnymi query parametrami oraz `X-Correlation-Id`.
2. Minimal API w `TripsEndpoints`:
   - odczytuje/generuje `X-Correlation-Id` i ustawia w odpowiedzi,
   - binduje parametry: `q`, `hasPlan`, `includeDeleted`, `limit`, `cursor`, `sort`,
   - mapuje do `ListTripsQueryRequest` (w tym `q -> Query`),
   - (docelowo) pozyskuje `userId` z `ClaimsPrincipal` i przekazuje do zapytania lub do serwisu kontekstu uzytkownika.
3. Endpoint wysyla `ListTripsQuery` przez `IMediator`.
4. `ListTripsQueryHandler`:
   - buduje `IQueryable<Trip>` na `_db.Trips.AsNoTracking()`,
   - (docelowo) filtruje po `UserId` (ownership),
   - stosuje filtry:
     - `q`: `title ILIKE %q% OR place_text ILIKE %q%` (przez `EF.Functions.ILike` w Npgsql),
     - `hasPlan`: `HasGeneratedPlan == true/false`,
     - `includeDeleted == false`: `DeletedAt == null` (jesli soft delete istnieje),
   - stosuje sortowanie na podstawie allow-listy pol,
   - stosuje keyset pagination (limit + cursor) w oparciu o sortKey + `Id` jako tie-breaker,
   - projektuje do `TripQueryModel` bez ladowania nawigacji (`TripTags`).
5. Handler zwraca `Result<ListTripsQueryResponse>`:
   - sukces: `200` z `{ items, nextCursor }`,
   - walidacja: `Result.Fail(VALIDATION_ERROR)` -> `400 ProblemDetails`.

## 6. Wzgledy bezpieczenstwa
- Autoryzacja (docelowo): JWT Bearer; endpoint prywatny.
- Wymuszenie ownership: zawsze filtruj `Trips` po `UserId` aktualnego uzytkownika (nie polegaj tylko na UI).
- Ochrona przed injection w `sort`: nie buduj dynamicznego `OrderBy` z stringa; uzyj mapowania allow-list (`switch` na dozwolone pola).
- Ograniczenia danych wejsciowych:
  - limit zakresu (`1..100`) chroni przed duzymi odpowiedziami,
  - ogranicz `q` (np. max 200 znakow), zeby uniknac drogich zapytan,
  - nie loguj pelnych danych wyszukiwania, jesli w przyszlosci beda zawierac dane wrazliwe.
- Cancelation: przekaz `CancellationToken` do EF (`ToListAsync`) zgodnie z `.cursor/rules/shared.mdc`.

## 7. Obsluga bledow
- `400 VALIDATION_ERROR`
  - nieznany `sort` lub zly prefix,
  - `limit` poza zakresem,
  - niepoprawny format `cursor` (np. nie da sie zdekodowac / brakuje wymaganych pol / niespojne z `sort`)
- `401 UNAUTHORIZED` (po wlaczeniu auth)
  - brak / niepoprawny JWT
- `500 INTERNAL_ERROR`
  - bledy DB, mapowania, inne nieoczekiwane wyjatki (obslugiwane przez `ExceptionHandlingMiddleware`)

Nie ma dedykowanej tabeli bledow w aktualnym planie DB; rejestruj w `ILogger` i polegaj na `traceId` w `ProblemDetails` (plus `X-Correlation-Id`).

## 8. Wydajnosc
- `AsNoTracking()` + projekcja `Select(...)` do `TripQueryModel` (bez ladowania relacji) minimalizuje koszty.
- Keyset pagination po (sortKey, Id) skaluje sie lepiej niz `Skip/Take` dla duzych zbiorow.
- Indeksy (docelowo, wg `.ai/db_plan.md`):
  - po `user_id` + `created_at` oraz `user_id` + `generated_at`
  - opcjonalnie (dla soft delete): `WHERE deleted_at IS NULL`
- `q` (ILIKE) moze byc kosztowne; dla MVP OK, ale przy wzroscie danych rozwaz:
  - `pg_trgm` + indeks trigramowy na `title`/`place_text`,
  - albo pelnotekstowe wyszukiwanie.

## 9. Kroki implementacji
1. **Doprecyzowanie kontraktu paginacji**
   - Ustal format `cursor` (rekomendacja: base64url(JSON) z polami: `sortField`, `sortDir`, `lastValue`, `lastId`).
   - Rozszerz wspolny kontrakt odpowiedzi stronicowanych tak, aby wspieral `nextCursor` (np. `PagedResponse<T>(Items, NextCursor)`), oraz dostosuj `ListTripsQueryResponse` do specyfikacji.
2. **Walidacja wejsciowa**
   - Dodaj `ListTripsQueryValidator` (FluentValidation) dla `ListTripsQuery` (lub `ListTripsQueryRequest`, jesli request jest opakowany w Result):
     - `limit` w `1..100` (domyslnie 20 w handlerze),
     - `sort` tylko z allow-list,
     - `cursor` poprawnie dekodowalny i zgodny z `sort`,
     - `q` max dlugosc (np. 200).
   - Zapewnij, ze query zwraca `Result<...>`, aby `ValidationBehavior` mogl zwrocic `Result.Fail` zamiast ignorowac walidacje.
3. **Handler MediatR**
   - Utworz `ListTripsQueryHandler : IRequestHandler<ListTripsQuery, Result<ListTripsQueryResponse>>`.
   - Zaimplementuj filtrowanie, sortowanie i keyset pagination:
     - allow-list mapping `sort` -> `Expression`/`IQueryable` ordering,
     - cursor decode -> predykat "after last row" zalezne od sort field i kierunku,
     - `Take(limit + 1)` dla wyliczenia `nextCursor`.
   - Dodaj projekcje do `TripQueryModel` z prawidlowym mapowaniem enumow (`BudgetLevel`, `Pace`) analogicznie do `CreateTripCommandHandler`.
4. **Soft delete a includeDeleted**
   - Jesli system ma wspierac `includeDeleted` zgodnie ze specyfikacja:
     - dodaj `DeletedAt` do encji `Trip`,
     - dodaj mapowanie EF (`deleted_at`) i migracje,
     - zaktualizuj `DeleteTrip` (gdy bedzie implementowany) na ustawianie `deleted_at` zamiast hard delete.
5. **Minimal API endpoint**
   - W `VibeTravelers.API.Endpoints.TripsEndpoints` dodaj:
     - `group.MapGet("/", ListTrips)` z `.Produces<ListTripsQueryResponse>(200)` i `.ProducesProblem(400)`
     - odczyt query params: `q`, `hasPlan`, `includeDeleted`, `limit`, `cursor`, `sort`
     - mapowanie do `ListTripsQueryRequest` i wyslanie przez `IMediator`
     - zwrot przez `result.ToHttpResult(...)` (zawsze zwracajac `{ items, nextCursor }` na sukcesie)
   - Na etapie developmentu, jesli autoryzacja jest pomijana jak w `CreateTrip`, uzyj tymczasowego `DevelopmentUserId` i filtruj query po nim, zeby nie przygotowac kodu, ktory przez przypadek ujawni dane.
6. **Testy**
   - Jednostkowe:
     - walidator `sort/limit/cursor`,
     - parser/serializer cursora (round-trip),
     - logika keyset pagination dla kazdego pola sortowania i kierunku.
   - Integracyjne:
     - `GET /trips` zwraca `200` i pusta liste dla braku danych,
     - filtrowanie `hasPlan=true/false`,
     - `q` filtruje po `title/placeText`,
     - `sort=-createdAt` i kolejne strony przez `cursor`,
     - `400 VALIDATION_ERROR` dla nieznanego `sort` i dla zlego `cursor`,
     - `includeDeleted=false` ukrywa soft-deleted (jesli soft delete jest wdrozony).

