# API Endpoint Implementation Plan: GET /tags

## 1. Przegląd punktu końcowego
Endpoint służy do zwracania listy globalnych tagów (`tag`), używanych m.in. przy preferencjach użytkownika i przy tworzeniu/oznaczaniu wycieczek. Punkt końcowy jest **publiczny** (brak wymogu JWT), ponieważ operuje wyłącznie na słowniku systemowym bez danych użytkownika. Zwraca listę tagów w formacie zgodnym ze specyfikacją (`id`, `code`, `displayName`, `createdAt`) z opcjonalną paginacją opartą o `cursor`.

## 2. Szczegóły żądania
- **Metoda HTTP**: `GET`
- **Struktura URL**: `/tags`
- **Parametry (query)**:
  - **Wymagane**: brak
  - **Opcjonalne**:
    - `limit` (`int?`): maksymalna liczba elementów na stronę; domyślnie np. `50`, dopuszczalny zakres `1–100`.
    - `cursor` (`string?`): znacznik pozycji do stronicowania kursorem (np. zakodowany `createdAt`/`id`).
    - `sort` (`string?`): pole sortowania i kierunek; dozwolone wartości:
      - `"createdAt"` / `"-createdAt"` – sortowanie po dacie utworzenia (rosnąco/malejąco),
      - `"code"` / `"-code"` – sortowanie po kodzie tagu (alfabetycznie).
- **Request Body**: brak (puste ciało).
- **Mapowanie do typów**:
  - `ListTagsQueryRequest(int? Limit, string? Cursor, string? Sort)` implementuje `IPagedRequest`, `ISortableRequest` i odpowiada parametrom query.

## 3. Wykorzystywane typy
- **DTO / modele zapytań**:
  - `TagQueryModel(Guid Id, string Code, string DisplayName, DateTimeOffset CreatedAt)` – model odczytu pojedynczego tagu, zgodny z `Tag DTO` ze specyfikacji.
  - `ListTagsQueryRequest` – opakowanie parametrów paginacji i sortowania.
  - `ListTagsQueryResponse(IReadOnlyList<TagQueryModel> Items, string? NextCursor)` dziedziczący po `PagedResponse<TagQueryModel>`.
  - `PagedResponse<T>(IReadOnlyList<T> Items, string? NextCursor)` – wspólny kontrakt odpowiedzi stronicowanych.
- **MediatR**:
  - `ListTagsQuery` – rekord zapytania `public sealed record ListTagsQuery(ListTagsQueryRequest Request);`.
  - `ListTagsQueryHandler` (do zaimplementowania) – handler `IRequestHandler<ListTagsQuery, ListTagsQueryResponse>`.
- **Warstwa domeny / EF Core**:
  - Nowa encja domenowa `Tag` w `VibeTravels.Domain.Entities.Tags` z polami:
    - `Id` (`Guid`),
    - `Code` (`string`),
    - `DisplayName` (`string`),
    - `CreatedAt` (`DateTimeOffset`).
  - Konfiguracja EF:
    - mapa do tabeli `tag`,
    - `Code` jako `text` z unikalnym indeksem,
    - `CreatedAt` jako `timestamp without time zone`.
  - Aktualizacja `IAppDbContext` / `AppDbContext`:
    - `DbSet<Tag> Tags { get; }`.

## 4. Szczegóły odpowiedzi
- **Sukces – 200 OK**:
  - Treść JSON:
    ```json
    {
      "items": [
        {
          "id": "uuid",
          "code": "museums",
          "displayName": "Museums",
          "createdAt": "timestamp"
        }
      ],
      "nextCursor": "optional-cursor-or-null"
    }
    ```
  - `items` – lista `TagQueryModel`.
  - `nextCursor` – opcjonalny znacznik do pobrania kolejnej strony; może być `null` lub całkowicie pominięty przez frontend, jeśli nie potrzebuje stronicowania.
- **Kody błędów**:
  - `400 VALIDATION_ERROR` – niepoprawne parametry (`limit`, `cursor`, `sort` – np. nieznane pole sortowania).
  - `500 INTERNAL_ERROR` – nieoczekiwane błędy serwera (zgodnie z globalnym middleware).
  - Dodatkowo, z punktu widzenia kontraktu:
    - `401` / `403` – nie są używane, ponieważ endpoint jest publiczny.
    - `404` – nie ma zastosowania dla listy; brak elementów oznacza pustą tablicę `items`.

## 5. Przepływ danych
1. Klient wywołuje `GET /tags` z opcjonalnymi parametrami `limit`, `cursor`, `sort`.
2. Minimal API w `TagsEndpoints` mapuje endpoint na metodę obsługi, która:
   - binduje parametry query do `ListTagsQueryRequest`,
   - tworzy `ListTagsQuery` i wysyła je przez `IMediator`.
3. MediatR uruchamia `ListTagsQueryHandler`.
4. `ListTagsQueryHandler`:
   - buduje zapytanie EF Core na `IAppDbContext.Tags`,
   - stosuje sortowanie zgodnie z `Request.Sort` (ograniczone do dozwolonych pól),
   - stosuje paginację (limit + cursor) na bazie `createdAt`/`id`,
   - projektuje wynik do `TagQueryModel` (projekcja LINQ po stronie DB),
   - oblicza `nextCursor` dla kolejnej strony.
5. Handler zwraca `ListTagsQueryResponse` do minimal API.
6. Endpoint minimal API mapuje odpowiedź do JSON (domyślna serializacja camelCase) i zwraca `200 OK` z `items` i `nextCursor`. Nagłówek `X-Correlation-Id` jest ustawiany na poziomie cross-cutting (np. zgodnie z istniejącym patternem z endpointu rejestracji).

## 6. Względy bezpieczeństwa
- Endpoint jest **publiczny**:
  - nie wymaga nagłówka `Authorization: Bearer <token>`,
  - nie wykonuje żadnej logiki związanej z użytkownikiem ani RLS (tabela `tag` jest globalnym słownikiem).
- Dane zwracane przez endpoint nie zawierają danych wrażliwych (jedynie kody i nazwy tagów).
- Zgodnie z zasadami:
  - brak bezpośredniej komunikacji z usługami AI,
  - wszystkie pola daty (`createdAt`) zwracane w formacie ISO z czasem w UTC.
- Rate limiting na tym endpointcie nie jest krytyczny (tylko odczyt globalnego słownika), ale może być objęty globalną polityką limitowania ruchu.

## 7. Obsługa błędów
- **Walidacja wejścia**:
  - FluentValidation dla `ListTagsQueryRequest`:
    - `Limit` > 0 i ≤ maksymalna wartość (np. 100),
    - `Cursor` – jeśli używany, poprawny format (np. parsowalny do `Guid` lub struktury kursora),
    - `Sort` – dozwolone wartości (`createdAt`, `-createdAt`, `code`, `-code`).
  - Przy błędach walidacji rzucany jest `ValidationException`, którą globalny middleware mapuje na:
    - `400 BadRequest` z `ProblemDetails` (`title = "VALIDATION_ERROR"`).
- **Pozostałe błędy**:
  - Błędy infrastruktury (DB, połączenie) przechwytywane przez `ExceptionHandlingMiddleware` i mapowane na:
    - `500 InternalServerError` z `ProblemDetails` (`title = "INTERNAL_ERROR"`).
  - Brak dedykowanej tabeli błędów w schemacie – korzystamy ze standardowego logowania aplikacyjnego (`ILogger`, `traceId` w `ProblemDetails`).

## 8. Rozważania dotyczące wydajności
- Tabela `tag` jest stosunkowo mała (słownik), więc:
  - zapytania są lekkie, ale i tak warto:
    - dodać indeks na `code` (już wymagany przez unikalność),
    - rozważyć indeks na `created_at` jeśli używamy go do sortowania/paginacji.
- Pagizacja kursorem:
  - unika problemów z przesunięciem danych przy paginacji offsetowej,
  - zmniejsza koszty przy dużej liczbie tagów (w przyszłości).
- Wszystkie operacje DB są asynchroniczne (`ToListAsync`, itp.), zgodnie z zasadami projektu.
- W przyszłości można dodać prostą warstwę cache (np. w pamięci) dla listy tagów, jeśli ich aktualizacja jest rzadka, a ruch bardzo duży – na razie nie jest to wymagane w MVP.

## 9. Kroki implementacji
1. **Model domenowy i EF Core**
   - Dodać encję `Tag` w projekcie domenowym (`VibeTravels.Domain.Entities.Tags`).
   - Dodać konfigurację EF (np. `TagConfiguration`) w projekcie infrastruktury:
     - mapa do tabeli `tag`,
     - unikalny indeks na `code`,
     - ustawienie domyślnych wartości `id` (`gen_random_uuid()`) i `created_at` (`now()`), jeśli korzystamy z DB defaultów.
   - Rozszerzyć `AppDbContext` i `IAppDbContext` o `DbSet<Tag> Tags`.
   - Wygenerować i zastosować migrację DB dla nowej tabeli/tagów (jeśli jeszcze nie istnieją).
2. **Warstwa aplikacyjna – query i modele**
   - Zweryfikować istniejące typy:
     - `TagQueryModel`,
     - `ListTagsQueryRequest`,
     - `ListTagsQueryResponse`,
     - `ListTagsQuery`.
   - Upewnić się, że przestrzenie nazw i lokalizacja są zgodne z regułami (`Features/Tags/Queries/Models`).
3. **Handler MediatR**
   - Utworzyć `ListTagsQueryHandler` w `VibeTravels.Application.Features.Tags.Queries` lub `Features/Tags/Handlers`:
     - implementacja `IRequestHandler<ListTagsQuery, ListTagsQueryResponse>`,
     - wstrzyknięcie `IAppDbContext`,
     - zbudowanie zapytania:
       - podstawowy `IQueryable<Tag>` z `Tags`,
       - zastosowanie sortowania według `Sort` (z ograniczeniem do dozwolonych wartości),
       - zastosowanie paginacji kursorem (np. `WHERE created_at > ...` dla sortowania rosnącego),
       - projekcja do `TagQueryModel`,
       - wyliczenie `NextCursor`.
4. **Walidacja**
   - Dodać `ListTagsQueryRequestValidator` (FluentValidation) w odpowiednim folderze:
     - reguły dla `Limit`, `Cursor`, `Sort` zgodnie z sekcją 7.
   - Upewnić się, że walidacja jest wpięta w pipeline MediatR (globalny behavior już powinien istnieć).
5. **Endpoint minimal API**
   - Dodać `TagsEndpoints` w projekcie API (`VibeTravelers.API.Endpoints`):
     - `var group = app.MapGroup("/tags").WithTags("Tags");`
     - `group.MapGet("/", ListTags)...` z:
       - `.WithName("ListTags")`,
       - `.Produces<ListTagsQueryResponse>(StatusCodes.Status200OK)`,
       - `.ProducesProblem(StatusCodes.Status400BadRequest)`,
       - `.AllowAnonymous();`
   - W `Program.cs` wywołać `app.MapTagsEndpoints();`.
   - W metodzie obsługi:
     - pobrać `limit`, `cursor`, `sort` z query,
     - utworzyć `ListTagsQueryRequest`,
     - wysłać `ListTagsQuery` przez `IMediator`,
     - ustawić `X-Correlation-Id` analogicznie do endpointu rejestracji,
     - zwrócić `Results.Ok(response)`.
6. **Obsługa błędów i logowanie**
   - Upewnić się, że `ExceptionHandlingMiddleware` jest stosowany (już jest w `Program.cs`).
   - Dodać ewentualne logowanie zapytań/odpowiedzi na poziomie middleware (bez logowania danych potencjalnie wrażliwych – tutaj jest tylko słownik).
7. **Testy**
   - Testy jednostkowe:
     - `ListTagsQueryRequestValidator` (scenariusze: poprawne/niepoprawne `limit`, `sort`, `cursor`),
     - `ListTagsQueryHandler` (sortowanie, paginacja, `nextCursor`).
   - Testy integracyjne:
     - wywołanie `GET /tags` na pustej bazie (zwraca pustą listę),
     - wywołanie z danymi – poprawne mapowanie do `TagQueryModel`,
     - obsługa błędnego `sort` (`400 VALIDATION_ERROR`),
     - sprawdzenie obecności nagłówka `X-Correlation-Id` w odpowiedzi.

