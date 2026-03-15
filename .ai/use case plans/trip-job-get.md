# API Endpoint Implementation Plan: GET /trips/{tripId}/generation-jobs

## 1. Przeglad punktu koncowego
Endpoint sluzy do listowania jobow generacji AI powiazanych z konkretnym tripem nalezacym do zalogowanego uzytkownika. Jest to endpoint typu read/query, wiec nie uruchamia generacji ani nie modyfikuje stanu domeny. Ma zwracac stronicowana liste jobow dla jednego `tripId`, posortowana po `requestedAt`, domyslnie malejaco.

Najwazniejsze obserwacje wzgledem aktualnego repo:
- kontrakty query juz istnieja w `Features/Jobs/Queries/`:
  - `ListTripGenerationJobsQuery`
  - `ListTripGenerationJobsQueryRequest`
  - `ListTripGenerationJobsQueryResponse`
  - `GenerationJobListItemQueryModel`
- brakuje implementacji handlera, walidatora, endpointu HTTP i logiki kursora dla tego use case;
- aktualna odpowiedz `ListTripGenerationJobsQueryResponse` nie przyjmuje jawnie `NextCursor`, wiec wymaga dopracowania, aby endpoint faktycznie wspieral paginacje zgodnie ze specyfikacja;
- domena i persistence uzywaja jeszcze statusow `pending/running`, podczas gdy kontrakt API wymaga `queued/processing`;
- encja `AiGenerationJob` i konfiguracja EF nie zawieraja jeszcze pelnego zestawu pol potrzebnych przez spec listowania, w szczegolnosci `Discarded`, `DiscardReason`, `AttemptNo`, `ErrorCode` oraz `UserId`.

## 2. Szczegoly zadania
- Metoda HTTP: `GET`
- Struktura URL: `/trips/{tripId}/generation-jobs`
- Naglowki:
  - wymagane docelowo: `Authorization: Bearer <token>`
  - opcjonalne: `X-Correlation-Id`
- Parametry route:
  - wymagane: `tripId` (`Guid`)
- Query params:
  - opcjonalne: `limit`
  - opcjonalne: `cursor`
  - opcjonalne: `sort`
- Dozwolone sortowanie:
  - `requestedAt`
  - `-requestedAt` jako domyslne
- Request Body:
  - brak

### Wymagane reguly walidacji
- `tripId` nie moze byc pustym `Guid`
- `limit`, jesli podany, powinien miescic sie w granicach globalnej konwencji API:
  - domyslnie `20`
  - maksimum `100`
- `sort` moze przyjmowac tylko `requestedAt` albo `-requestedAt`
- `cursor`, jesli podany, musi byc zgodny z formatem kursora dla sortowania po `requestedAt`
- trip musi istniec, nalezec do aktualnego usera i nie byc soft-deleted
- dla cudzego lub nieistniejacego tripa endpoint ma zwracac `404 TRIP_NOT_FOUND`

## 3. Wykorzystywane typy
### Istniejace typy do wykorzystania lub dopracowania
- `ListTripGenerationJobsQuery`
- `ListTripGenerationJobsQueryRequest`
- `ListTripGenerationJobsQueryResponse`
- `GenerationJobListItemQueryModel`
- `GenerationJobStatus`
- `IAppDbContext`
- `AiGenerationJob`
- `AiGenerationJobStatus`
- `ResultErrors.TripNotFound(...)`

### Typy do dodania lub rozszerzenia
- `ListTripGenerationJobsQueryValidator`
- `ListTripGenerationJobsQueryHandler`
- helper kursora, np. `ListTripGenerationJobsCursor`
- rozszerzenie `ListTripGenerationJobsQueryResponse`, aby konstruktor przyjmowal `NextCursor`
- opcjonalnie maly wspoldzielony mapper read-modelu, jesli zespol chce uniknac duplikacji mapowania statusow miedzy:
  - `GET /generation-jobs/{jobId}`
  - `GET /trips/{tripId}/generation-jobs`

### Modele persistence wymagajace domkniecia wzgledem spec
Aktualna implementacja `AiGenerationJob` nie jest jeszcze zgodna z planem API i DB. Aby endpoint byl zgodny ze specyfikacja 7.3 oraz przygotowany pod 7.2 i worker concurrency rule, trzeba dopelnic:
- `UserId`
- `AttemptNo`
- `ErrorCode`
- `Discarded`
- `DiscardReason`
- ewentualnie ujednolicic nazwy payloadu:
  - repo ma `InputSnapshot`
  - `db_plan` zaklada `request_payload` i `response_payload`

## 4. Przeplyw danych
1. Klient wywoluje `GET /trips/{tripId}/generation-jobs?limit=&cursor=&sort=`.
2. Minimal API:
   - odczytuje `X-Correlation-Id` lub generuje nowy,
   - binduje `tripId`, `limit`, `cursor`, `sort`,
   - pobiera `userId` z kontekstu auth; do czasu wdrozenia JWT moze pozostac tymczasowy `DevelopmentUserId`, zgodnie z obecnym projektem.
3. Endpoint wysyla `ListTripGenerationJobsQuery` przez `IMediator`.
4. `ListTripGenerationJobsQueryValidator` sprawdza:
   - `TripId != Guid.Empty`
   - poprawny `limit`
   - poprawny `sort`
   - zgodnosc `cursor` z requested sort
5. `ListTripGenerationJobsQueryHandler` najpierw weryfikuje istnienie i ownership tripa:
   - `trip.Id == request.Request.TripId`
   - `trip.UserId == currentUserId`
   - `trip.DeletedAt == null`
6. Jesli trip nie istnieje, handler zwraca `Result.Fail(ResultErrors.TripNotFound(...))`.
7. Handler buduje zapytanie do `_db.AiGenerationJobs`:
   - `AsNoTracking()`
   - filtr po `TripId`
   - docelowo dodatkowo po `UserId`, jesli kolumna zostanie dopieta do encji
8. Handler stosuje cursor pagination oparta o:
   - `requestedAt`
   - tie-breaker `id`
9. Handler pobiera `limit + 1` rekordow, zeby wyliczyc `nextCursor`.
10. Handler mapuje rekordy na `GenerationJobListItemQueryModel`:
    - `Pending -> Queued`
    - `Running -> Processing`
    - pozostale statusy bez zmian
11. Handler zwraca `ListTripGenerationJobsQueryResponse(items, nextCursor)`.
12. Endpoint mapuje `Result` na:
    - `200 OK` z `{ items, nextCursor }`
    - odpowiedni `ProblemDetails` dla bledow.

### Rekomendowany ksztalt zapytania SQL/EF
- pojedynczy select bez `Include`
- projection bez ladowania zbednych encji
- sortowanie:
  - domyslnie `requested_at DESC, id DESC`
  - alternatywnie `requested_at ASC, id ASC`

To pozwala utrzymac endpoint tani dla pollingu i widoku historii jobow.

## 5. Wzgledy bezpieczenstwa
- Endpoint powinien byc chroniony JWT bearer auth; aktualne `.AllowAnonymous()` w endpointach nalezy traktowac jako etap przejsciowy.
- Ownership musi byc sprawdzane na poziomie handlera:
  - po `tripId + userId`
  - opcjonalnie rowniez po `job.UserId`, gdy model persistence zostanie uzupelniony
- Dla cudzego `tripId` nalezy zwracac `404`, nie `403`, aby nie ujawniac istnienia zasobu.
- Endpoint nie powinien zwracac danych wewnetrznych joba:
  - `InputSnapshot`
  - payloadow request/response do AI
  - hashy wejscia
- Logi aplikacyjne nie powinny serializowac calych encji jobow ani payloadow AI.
- Wszystkie operacje I/O musza byc async i honorowac `CancellationToken`.
- `X-Correlation-Id` trzeba zachowac w odpowiedzi dla latwiejszego laczenia requestow listowania z innymi logami systemu.

## 6. Obsluga bledow
### Oczekiwane kody stanu
- `200 OK`
  - lista jobow zostala poprawnie zwrocona
- `400 VALIDATION_ERROR`
  - pusty `tripId`
  - niepoprawny `limit`
  - nieobslugiwany `sort`
  - niepoprawny format `cursor`
- `401 UNAUTHORIZED`
  - brak lub niepoprawny token po wlaczeniu auth
- `404 TRIP_NOT_FOUND`
  - trip nie istnieje
  - trip nalezy do innego usera
  - trip jest soft-deleted i nie powinien byc widoczny
- `500 INTERNAL_ERROR` / `UNKNOWN_ERROR`
  - nieoczekiwany blad EF Core
  - niespojnosc danych w kolumnie `status`
  - inny nieobslugiwany blad runtime

### Rejestrowanie bledow
Ten endpoint jest tylko do odczytu, wiec nie wymaga zapisu do osobnej tabeli bledow. Nalezy korzystac z:
- `Result` / `Result<T>` dla bledow oczekiwanych
- `ResultHttpMapper` do mapowania na `ProblemDetails`
- `ExceptionHandlingMiddleware` dla bledow nieoczekiwanych

Nie ma potrzeby dodawania wpisow do `audit_event` ani osobnej tabeli bledow dla tego use case.

## 7. Wydajnosc
- Uzywac `AsNoTracking()` dla calego odczytu.
- Wykonac projection bez materializacji encji `Trip`.
- Ograniczyc odpowiedz do `limit + 1` rekordow.
- Cursor pagination jest preferowana nad offset pagination, bo lepiej skaluje sie dla historii jobow.
- Utrzymac stabilne sortowanie po `requestedAt` z tie-breakerem `Id`.
- Korzystac z indeksu po `trip_id, requested_at DESC`, ktory jest zgodny z planem DB.
- Jesli zostanie dodane filtrowanie po `userId` bezposrednio na `ai_generation_job`, warto rozwazyc indeks wspierajacy listowanie per user/trip, ale dla MVP indeks `trip_id + requested_at` powinien byc wystarczajacy.

## 8. Etapy wdrozenia
1. Domknac kontrakt odpowiedzi query
   - zmienic `ListTripGenerationJobsQueryResponse`, aby przyjmowal `Items` oraz `NextCursor`
   - upewnic sie, ze JSON ma ksztalt zgodny ze spec:
     - `items`
     - `nextCursor`

2. Dodac walidator query
   - utworzyc `ListTripGenerationJobsQueryValidator`
   - zwalidowac `TripId`, `Limit`, `Sort`, `Cursor`
   - dopasowac styl walidacji do istniejacego `ValidationBehavior`

3. Dodac helper kursora
   - utworzyc `ListTripGenerationJobsCursor`
   - obsluzyc tylko jedno dozwolone pole sortowania: `requestedAt`
   - zakodowac w cursorze:
     - direction sortowania
     - ostatnie `requestedAt`
     - ostatnie `id`
   - zapewnic walidacje zgodnosci cursora z requested sort

4. Zaimplementowac handler MediatR
   - dodac `ListTripGenerationJobsQueryHandler`
   - sprawdzic ownership tripa przez `_db.Trips`
   - wykonac `AsNoTracking()` query po `_db.AiGenerationJobs`
   - zastosowac sorting, cursor i `Take(limit + 1)`
   - zmapowac rekordy na `GenerationJobListItemQueryModel`
   - zwrocic `Result<ListTripGenerationJobsQueryResponse>`

5. Ujednolicic mapowanie statusow API
   - wydzielic wspolna metode lub maly mapper:
     - `Pending -> Queued`
     - `Running -> Processing`
   - uzyc tego samego mapowania w `QueueGenerationJobCommandHandler`, `GetGenerationJobById` i nowym listowaniu

6. Dopiac endpoint Minimal API
   - rozszerzyc `TripsEndpoints` o `GET /{tripId:guid}/generation-jobs`
   - przyjac `limit`, `cursor`, `sort` przez `[FromQuery]`
   - ustawic `X-Correlation-Id`
   - zmapowac `Result` przez `ToHttpResult(...)`
   - dodac:
     - `.Produces<ListTripGenerationJobsQueryResponse>(200)`
     - `.ProducesProblem(400)`
     - `.ProducesProblem(401)`
     - `.ProducesProblem(404)`

7. Rozwiazac luki modelu danych dla jobow
   - porownac:
     - spec 7.2 i 7.3
     - `db_plan`
     - aktualna encje `AiGenerationJob`
     - `AiGenerationJobConfiguration`
   - dopiac brakujace pola co najmniej potrzebne do listowania i wspolnego read-modelu:
     - `UserId`
     - `Discarded`
     - `DiscardReason`
   - zaplanowac migracje EF Core

8. Dodac testy jednostkowe
   - validator:
     - pusty `TripId`
     - zbyt duzy `Limit`
     - niepoprawny `Sort`
     - niepoprawny `Cursor`
   - handler:
     - `TRIP_NOT_FOUND` dla nieistniejacego tripa
     - `TRIP_NOT_FOUND` dla cudzego tripa
     - sukces z domyslnym sortowaniem malejacym
     - sukces z `requestedAt` rosnaco
     - poprawne wyliczenie `NextCursor`
     - poprawne mapowanie `Pending/Running`

9. Dodac testy integracyjne
   - `GET /trips/{tripId}/generation-jobs` zwraca `200`
   - odpowiedz ma `items` i `nextCursor`
   - odpowiedz zawiera `X-Correlation-Id`
   - kolejnosc dla domyslnego `-requestedAt` jest poprawna
   - pagination po cursorze dziala stabilnie dla rekordow z tym samym `requestedAt`
   - nieistniejacy trip zwraca `404`
   - cudzy trip zwraca `404`
   - niepoprawny `sort` zwraca `400`

10. Posprzatac rozjazdy nazewnicze w persistence
   - obecna tabela EF mapuje sie do `generation_jobs`, a `db_plan` zaklada `ai_generation_job`
   - obecne statusy persistence to `pending/running`, a spec wymaga `queued/processing`
   - nalezy jawnie zdecydowac, czy:
     - zostawiamy statusy domenowe i tylko mapujemy je do kontraktu API
     - czy migrujemy persistence do nazewnictwa zgodnego ze spec
   - plan implementacji endpointu powinien przyjac jedna, spojna strategia i stosowac ja w calym feature `Jobs`


Komentarze architekta (nadpisują to co było napisane wyżej):
- nie robimy na razie autoryzacji
- sortowanie tylko po dacie od najnowszych i nie przyjmujemy sortowania jako parametr
