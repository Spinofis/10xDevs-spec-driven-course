# API Endpoint Implementation Plan: PUT /trips/{tripId}/plan

## 1. Przeglad punktu koncowego
Endpoint sluzy do pelnej, manualnej podmiany istniejacego planu wycieczki dla wskazanego `tripId`. Jest to endpoint typu command/write i powinien zwracac `200 OK` z kompletnym `Plan DTO` po zapisaniu nowej wersji planu.

Najwazniejsze zalozenia wdrozeniowe:
- endpoint nie tworzy nowego zasobu planu od zera; jezeli `trip` istnieje, ale plan nie istnieje, nalezy zwrocic `404 PLAN_NOT_FOUND`;
- endpoint zastepuje caly plan atomowo: naglowek planu oraz cala kolekcje pozycji;
- po manualnej edycji plan powinien miec `status = saved`, `savedAt = now(UTC)` oraz zwiekszony `version`;
- `generatedFromJobId` i `generatedAt` powinny zostac zachowane z poprzedniego planu, jezeli plan powstal w wyniku generacji AI; to jest konieczna decyzja implementacyjna, bo spec nie opisuje jawnie resetowania tych pol;
- request nie zawiera `item.id`, wiec po replace wszystkie pozycje planu dostaja nowe identyfikatory i nowe znaczniki `createdAt` oraz `updatedAt`.

Stan repo, ktory trzeba uwzglednic w planie:
- istnieja juz szkice kontraktow Application dla `UpdatePlanCommand`, `GetPlanByTripIdQuery`, `PlanQueryModel` i `PlanItemCommandModel`;
- nie ma jeszcze handlerow, endpointow Minimal API ani persistence modelu dla strukturalnego planu;
- repo jest w trakcie migracji z namespace `Legacy` do nowszego feature `Plans`, wiec wdrozenie powinno domknac nowy feature zamiast dokladac kolejny tor obok `Legacy`;
- `.ai/db_plan.md` opisuje uproszczony `trip_plan.current_text`, ale aktualne modele API i starszy SQL bazowy zakladaja plan strukturalny z pozycjami; przed implementacja trzeba jawnie wybrac jedna wersje persistence.

Rekomendacja architektoniczna:
- wdrozyc persistence zgodny z obecnym `Plan DTO`, nie z `current_text`;
- wykorzystac naglowek planu plus osobna tabele pozycji, bo tylko taki model pozwala poprawnie obsluzyc `version`, `status`, `savedAt`, `generatedAt`, `item.id`, `item.updatedAt` oraz `tags`.

## 2. Szczegoly zadania
- Metoda HTTP: `PUT`
- Struktura URL: `/trips/{tripId}/plan`
- Naglowki:
  - docelowo wymagane: `Authorization: Bearer <token>`
  - opcjonalne: `X-Correlation-Id`
- Parametry:
  - wymagane: `tripId` (`Guid`) w route
  - opcjonalne: brak query string

### Request body
```json
{
  "summary": "string|null",
  "items": [
    {
      "dayNumber": 1,
      "order": 10,
      "title": "string",
      "description": "string|null",
      "locationText": "string|null",
      "startTime": "HH:mm|null",
      "endTime": "HH:mm|null",
      "durationMinutes": 90,
      "costLevel": "low|medium|high|null",
      "tags": ["culture"]
    }
  ]
}
```

### Parametry wymagane i opcjonalne
- Wymagane:
  - `tripId`
  - `items`
  - dla kazdej pozycji: `dayNumber`, `order`, `title`, `durationMinutes`
- Opcjonalne:
  - `summary`
  - dla kazdej pozycji: `description`, `locationText`, `startTime`, `endTime`, `costLevel`, `tags`

### Wymagane reguly walidacji
- `tripId` nie moze byc pustym `Guid`
- `items` nie moze byc `null`
- `items[].title` jest wymagane i nie moze byc puste po `Trim()`
- `items[].dayNumber >= 1`
- `items[].order` jest wymagane
- `items[].startTime` i `items[].endTime`, jezeli sa podane, musza miec format `HH:mm`
- `items[].costLevel`, jezeli jest podane, musi nalezec do `low|medium|high`

Walidacje, ktore wynikaja z DTO i powinny zostac doprecyzowane w kodzie:
- `durationMinutes` w spec jest polem wymaganym i nie-null, ale obecny `PlanItemCommandModel` ma `int?`; rekomendacja: ujednolicic kontrakt do pola wymaganego i walidowac `> 0`;
- jezeli `order` ma byc dozwolonym `0`, obecny model wejscia musi umiec odroznic "brak pola" od "wartosc 0"; samo `int` nie daje tej mozliwosci podczas bindowania JSON;
- `tags` najlepiej normalizowac do pustej listy zamiast `null`, bo odpowiedz DTO zawsze zwraca tablice.

### Wymagane typy i modele
- Istniejace do wykorzystania lub korekty:
  - `UpdatePlanCommand`
  - `UpdatePlanCommandRequest`
  - `UpdatePlanCommandResponse`
  - `UpdatePlanCommandModel`
  - `PlanItemCommandModel`
  - `PlanQueryModel`
  - `PlanItemQueryModel`
  - `GetPlanByTripIdQuery`
  - `GetPlanByTripIdQueryResponse`
- Nowe elementy Application:
  - `UpdatePlanCommandHandler`
  - `UpdatePlanCommandRequestValidator`
  - `UpdatePlanCommandModelValidator`
  - `PlanItemCommandModelValidator`
- Nowe elementy Domain / Infrastructure:
  - `TripPlan`
  - `TripPlanItem`
  - opcjonalnie `TripPlanMapper` lub podobny komponent projekcyjny
- Nowe bledy domenowe:
  - `PLAN_NOT_FOUND`

### Wyodrebnienie logiki do service
Handler nie powinien zawierac calej logiki replace, wersjonowania i mapowania pozycji. Zalecany podzial:
- `UpdatePlanCommandHandler`:
  - ownership check,
  - decyzja `TRIP_NOT_FOUND` vs `PLAN_NOT_FOUND`,
  - transakcja,
  - wywolanie serwisu,
  - zapis i zwrot DTO;
- nowy serwis, np. `ITripPlanWriteService`:
  - normalizacja `summary` i pol tekstowych itemow,
  - walidacja i przygotowanie nowych `TripPlanItem`,
  - wyliczenie nowego `version`,
  - ustawienie `status = saved` i `savedAt`,
  - atomowa wymiana calej kolekcji itemow;
- wspolny mapper, np. `IPlanReadModelMapper`:
  - mapowanie encji persistence do `PlanQueryModel`,
  - wspolna logika dla `PUT /trips/{tripId}/plan` i przyszlego `GET /trips/{tripId}/plan`.

## 3. Szczegoly odpowiedzi
- `200 OK`
```json
{
  "tripId": "uuid",
  "version": 4,
  "status": "saved",
  "generatedFromJobId": "uuid|null",
  "generatedAt": "timestamp|null",
  "savedAt": "timestamp",
  "summary": "string|null",
  "items": [
    {
      "id": "uuid",
      "dayNumber": 1,
      "order": 10,
      "title": "string",
      "description": "string|null",
      "locationText": "string|null",
      "startTime": "HH:mm|null",
      "endTime": "HH:mm|null",
      "durationMinutes": 90,
      "costLevel": "low|medium|high|null",
      "tags": ["culture"],
      "createdAt": "timestamp",
      "updatedAt": "timestamp"
    }
  ]
}
```

### Zasady mapowania odpowiedzi
- `status` po sukcesie powinien byc zawsze `saved`
- `version` powinien zostac zwiekszony o `1` wzgledem poprzedniego planu
- `savedAt` powinien byc ustawiony na aktualny czas UTC
- `generatedFromJobId` oraz `generatedAt` powinny zostac zachowane z poprzedniego planu, jezeli byly ustawione
- wszystkie nowe pozycje powinny dostac nowe `id`
- `createdAt` i `updatedAt` dla nowych pozycji powinny byc ustawione podczas replace
- odpowiedz powinna byc serializowana w `camelCase`
- timestampy powinny byc w UTC zgodnie z reguami projektu

### Zasady formatowania czasu
Spec wymaga `HH:mm`, a obecne modele korzystaja z `TimeOnly?`. Domyslna serializacja `TimeOnly` w .NET nie gwarantuje zgodnosci ze spec, dlatego nalezy dodac wlasny `JsonConverter<TimeOnly>` i `JsonConverter<TimeOnly?>` dla:
- requestu `PUT /trips/{tripId}/plan`
- odpowiedzi `PlanQueryModel`

## 4. Przeplyw danych
1. Klient wysyla `PUT /trips/{tripId}/plan` z body zawierajacym caly nowy plan.
2. Minimal API w `TripsEndpoints`:
   - odczytuje lub generuje `X-Correlation-Id`,
   - binduje `tripId` z route,
   - binduje body do `UpdatePlanCommandRequest`,
   - pobiera `userId` z auth; do czasu wdrozenia auth moze tymczasowo korzystac z `TripsEndpoints.DevelopmentUserId`.
3. Endpoint wysyla `UpdatePlanCommand` przez `IMediator`.
4. FluentValidation wykonuje walidacje skladniowa:
   - `TripId != Guid.Empty`
   - poprawny body shape
   - poprawne wartosci pol itemow
   - poprawny format czasu `HH:mm`
5. Handler pobiera `Trip` po `tripId + userId` i filtruje rekordy soft-deleted.
6. Jezeli `Trip` nie istnieje, handler zwraca `404 TRIP_NOT_FOUND`.
7. Handler pobiera istniejacy plan wraz z pozycjami.
8. Jezeli `Trip` istnieje, ale plan nie istnieje, handler zwraca `404 PLAN_NOT_FOUND`.
9. `ITripPlanWriteService` normalizuje dane:
   - `summary`, `title`, `description`, `locationText` po `Trim()`
   - `tags` do listy bez `null`
   - `costLevel` do enum lub stalej zgodnej z kontraktem
10. W jednej transakcji serwis:
   - aktualizuje naglowek planu (`summary`, `status`, `savedAt`, `version`, `updatedAt`),
   - usuwa poprzednie pozycje planu,
   - wstawia nowa kolekcje pozycji planu,
   - zapisuje wszystkie zmiany jednym `SaveChangesAsync`.
11. Handler mapuje zaktualizowany plan do `PlanQueryModel`.
12. Endpoint zwraca `200 OK` z pelnym `Plan DTO`.

### Rekomendowany model persistence
Obecna specyfikacja API nie da sie poprawnie odwzorowac w uproszczonym `trip_plan.current_text`. Z tego powodu rekomendowany jest model strukturalny:
- tabela `trip_plans`
  - `trip_id`
  - `user_id`
  - `version`
  - `status`
  - `generated_from_job_id`
  - `generated_at`
  - `saved_at`
  - `summary`
  - `created_at`
  - `updated_at`
- tabela `trip_plan_items`
  - `id`
  - `trip_id`
  - `day_number`
  - `order`
  - `title`
  - `description`
  - `location_text`
  - `start_time`
  - `end_time`
  - `duration_minutes`
  - `cost_level`
  - `tags` jako `text[]`
  - `created_at`
  - `updated_at`

Powod rekomendacji:
- `PlanQueryModel` jest juz strukturalny;
- request i response operuja na kolekcji itemow;
- item ma wlasne `id`, `createdAt`, `updatedAt`, ktorych nie da sie odzyskac z pola tekstowego bez dodatkowego parsera;
- `tags` sa per item, a nie per caly plan.

## 5. Wzgledy bezpieczenstwa
- Endpoint docelowo musi byc chroniony JWT Bearer, zgodnie z zasadami projektu.
- Ownership musi byc sprawdzany w handlerze, nie tylko w endpointzie.
- Dla cudzego `tripId` nalezy zwracac `404 TRIP_NOT_FOUND`, nie `403`, aby nie ulatwiac enumeracji zasobow.
- Nie wolno przyjmowac `userId` z body ani z query string.
- Wszystkie dane tekstowe planu nalezy traktowac jako plain text; backend nie powinien zapisywac ani renderowac HTML.
- Frontend powinien wyswietlac te pola w sposob bezpieczny dla XSS; backend nie powinien zakladac, ze `description` jest zaufanym HTML.
- Logi musza redagowac dane wrazliwe; logowac nalezy `tripId`, `userId`, `traceId`, `correlationId`, ale nie pelna tresc `summary` ani `description`.
- Wszystkie operacje DB musza byc async i przyjmowac `CancellationToken`.
- Ze wzgledu na potencjalnie duzy payload nalezy ustawic rozsadne limity dlugosci dla pol tekstowych i liczby pozycji planu; jesli biznes nie dostarczyl limitow, trzeba je doprecyzowac przed wdrozeniem lub przyjac konserwatywne wartosci aplikacyjne.

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - `tripId` jest pustym `Guid`
  - `items` jest `null`
  - `items[].title` jest puste
  - `items[].dayNumber < 1`
  - `items[].order` nie zostalo przekazane lub nie przechodzi walidacji kontraktu
  - `items[].startTime` lub `items[].endTime` ma niepoprawny format
  - `items[].costLevel` jest spoza dozwolonego zbioru
  - `items[].durationMinutes` jest `null` albo `<= 0` po doprecyzowaniu kontraktu
  - body nie daje sie zdeserializowac
- `404 TRIP_NOT_FOUND`
  - trip nie istnieje
  - trip nalezy do innego usera
  - trip jest soft-deleted
- `404 PLAN_NOT_FOUND`
  - trip istnieje, ale nie ma powiazanego planu do manualnej podmiany
- `401 UNAUTHORIZED`
  - brak lub niepoprawny token po wlaczeniu auth
- `500 INTERNAL_ERROR`
  - nieoczekiwany blad EF Core
  - blad transakcji
  - inny nieoczekiwany blad runtime

### Rejestrowanie bledow
Ten endpoint nie korzysta z asynchronicznego joba, wiec zapis bledow do `ai_generation_job.error_code` nie ma tu zastosowania. Aktualny model danych nie ma tez dedykowanej tabeli bledow technicznych. Rekomendacja:
- expected failures obslugiwac przez `Result` + `ProblemDetails`;
- unexpected failures logowac przez `ILogger` i `ExceptionHandlingMiddleware`;
- nie dodawac osobnej tabeli bledow tylko dla tego endpointu;
- opcjonalny `audit_event` moze w przyszlosci sluzyc do sledzenia biznesowego "plan manually updated", ale nie jako storage bledow technicznych.

### Niezbedne rozszerzenia bledow
Nalezy dodac do `ResultErrors`:
- `PLAN_NOT_FOUND` ze statusem `404`

## 7. Wydajnosc
- Plan i jego pozycje powinny byc ladowane jednym zapytaniem, bez N+1.
- Replace pozycji musi byc wykonany w jednej transakcji, aby uniknac stanu posredniego "naglowek zapisany, itemy nie".
- Jezeli implementacja korzysta z strategii `delete + insert`, nalezy ograniczyc liczbe round-tripow do bazy i zapisac wszystko jednym `SaveChangesAsync`.
- Dla odczytu planu i kolejnosci pozycji warto utrzymac indeksy:
  - po `trip_id` na `trip_plans`
  - po `trip_id, day_number, order` na `trip_plan_items`
- Warto wprowadzic optimistic concurrency dla naglowka planu, bo endpoint wykonuje pelny replace i jest podatny na "last write wins" przy rownoleglych edycjach. Spec PUT nie dostarcza jeszcze `If-Match`, ale kolumna `version` powinna byc aktualizowana atomowo i przygotowana pod przyszle zabezpieczenie przed utrata zmian.
- Nie nalezy wykonywac dodatkowych projekcji lub sortowan w pamieci, jezeli kolejnosc itemow moze byc ustalona w SQL.

## 8. Kroki implementacji
1. Uzgodnic i utrwalic model persistence planu
   - Odrzucic wariant `current_text` dla tego endpointu.
   - Przyjac strukturalny model zgodny z `PlanQueryModel`.
   - Uzgodnic, czy rozbudowujemy istniejacy stary SQL `trip_plans/plan_items`, czy tworzymy nowe tabele w aktualnej migracji EF.
2. Domknac kontrakty Application
   - Zmienic `UpdatePlanCommand` tak, aby implementowal `IRequest<Result<UpdatePlanCommandResponse>>`.
   - Dodac `UserId` do commandu, analogicznie do pozostalych write endpointow.
   - Doprecyzowac `PlanItemCommandModel` pod katem pol wymaganych, zwlaszcza `order` i `durationMinutes`.
3. Dodac walidatory FluentValidation
   - `UpdatePlanCommandRequestValidator`
   - `UpdatePlanCommandModelValidator`
   - `PlanItemCommandModelValidator`
   - Walidacja musi obslugiwac `HH:mm`, `dayNumber >= 1`, `title` required i enum `costLevel`.
4. Dodac persistence i konfiguracje EF Core
   - `TripPlan`
   - `TripPlanItem`
   - `DbSet<>` w `IAppDbContext` i `AppDbContext`
   - konfiguracje EF
   - migracja z indeksami i ograniczeniami
5. Dodac serwis replace planu
   - `ITripPlanWriteService`
   - logika wersjonowania
   - logika `savedAt`
   - replace calej kolekcji pozycji
   - zachowanie `generatedFromJobId` i `generatedAt`
6. Zaimplementowac `UpdatePlanCommandHandler`
   - pobranie `Trip`
   - rozroznienie `TRIP_NOT_FOUND` i `PLAN_NOT_FOUND`
   - uruchomienie serwisu
   - zapis transakcyjny
   - mapowanie do `UpdatePlanCommandResponse`
7. Zaimplementowac lub domknac odczyt planu
   - `GetPlanByTripIdQueryHandler`
   - wspolny mapper do `PlanQueryModel`
   - uporzadkowanie wspolpracy miedzy nowym feature `Plans` i starym `Legacy`
8. Dodac endpoint Minimal API
   - `group.MapPut("/{tripId:guid}/plan", UpdatePlan)`
   - `.Produces<UpdatePlanCommandResponse>(200)`
   - `.ProducesProblem(400)`
   - `.ProducesProblem(401)`
   - `.ProducesProblem(404)`
   - zachowac `X-Correlation-Id`
9. Dodac konwertery JSON dla czasu
   - wlasny converter `TimeOnly` i `TimeOnly?`
   - rejestracja w `Program.cs`
   - testy serializacji i deserializacji dla formatu `HH:mm`
10. Dodac testy
   - jednostkowe walidatorow
   - jednostkowe serwisu replace
   - testy handlera dla `TRIP_NOT_FOUND`
   - testy handlera dla `PLAN_NOT_FOUND`
   - testy handlera dla sukcesu z inkrementacja `version`
   - testy integracyjne endpointu `PUT /trips/{tripId}/plan`
   - test odpowiedzi z poprawnym formatem `HH:mm`
