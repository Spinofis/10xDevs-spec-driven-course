# API Endpoint Implementation Plan: GET /trips/{tripId}/plan

## 1. Przeglad punktu koncowego
Endpoint sluzy do pobrania aktualnego planu wycieczki dla istniejacego `trip`. Jest to czysty endpoint read/query: nie uruchamia generacji AI, nie modyfikuje stanu i zwraca ostatni zapisany plan w ksztalcie zgodnym z `Plan DTO`.

Najwazniejsze zalozenia wdrozeniowe:
- endpoint musi rozrozniac dwa przypadki `404`:
  - `TRIP_NOT_FOUND`, gdy `trip` nie istnieje, jest soft-deleted albo nie nalezy do usera;
  - `PLAN_NOT_FOUND`, gdy `trip` istnieje i nalezy do usera, ale plan nie zostal jeszcze zapisany;
- ownership musi byc sprawdzane po `tripId + userId`, zgodnie z zasadami backendu i security;
- odpowiedz ma byc budowana z persistence, nie z danych wyliczanych ad hoc;
- Minimal API powinno delegowac logike do MediatR query;
- wszystkie operacje I/O musza byc async i przyjmowac `CancellationToken`.

Istotny stan obecnego repo, ktory trzeba uwzglednic w planie:
- istnieja szkielety `GetPlanByTripIdQuery`, `GetPlanByTripIdQueryRequest`, `GetPlanByTripIdQueryResponse`, `PlanQueryModel` i `PlanItemQueryModel`;
- obecny `GetPlanByTripIdQuery` nie niesie `UserId`, wiec nie da sie w nim poprawnie wymusic ownership;
- nie istnieje jeszcze endpoint Minimal API dla `/trips/{tripId}/plan`;
- `AppDbContext` i `IAppDbContext` nie eksponuja jeszcze persistence dla planu;
- fizyczne tabele `trip_plans` i `plan_items` istnieja w `20260116170705_vibe_trvelers.sql`, ale ich aktualny ksztalt nie pokrywa calego kontraktu API 8.1;
- dokument `.ai/db_plan.md` opisuje starszy model tekstowy `trip_plan`, podczas gdy `.ai/api_plan v2.md` i komentarz architekta wskazuja model strukturalny oparty o `trip_plans` + `plan_items`.

Rekomendacja architektoniczna dla tego endpointu:
- jako source of truth przyjac model strukturalny planu;
- doprowadzic persistence i EF Core do zgodnosci z istniejacymi tabelami SQL oraz kontraktem `Plan DTO`;
- potraktowac opis tekstowego `trip_plan.current_text` w `.ai/db_plan.md` jako nieaktualny dla implementacji endpointow planu.

## 2. Szczegoly zadania
- Metoda HTTP: `GET`
- Struktura URL: `/trips/{tripId}/plan`
- Naglowki:
  - docelowo wymagane: `Authorization: Bearer <token>`
  - opcjonalne: `X-Correlation-Id`
- Parametry:
  - wymagane: `tripId` (`Guid`) w route
  - opcjonalne: brak
- Request Body:
  - brak

### Wymagane reguly walidacji
- `tripId` nie moze byc pustym `Guid`
- `trip` musi istniec, nalezec do aktualnego usera i nie byc soft-deleted
- plan dla tego `tripId` musi istniec
- plan items musza byc odczytywane w stabilnej kolejnosci zgodnej z kontraktem (`dayNumber`, potem `order`)
- pola odpowiedzi musza byc mapowane do formatu API:
  - `status` jako `generated|saved`
  - godziny jako `"HH:mm"` po serializacji `TimeOnly`
  - enumy jako `camelCase`

### Wymagane typy i modele
- istniejace kontrakty Application do dopracowania:
  - `GetPlanByTripIdQuery`
  - `GetPlanByTripIdQueryRequest`
  - `GetPlanByTripIdQueryResponse`
  - `PlanQueryModel`
  - `PlanItemQueryModel`
- rekomendowane nowe lub brakujace elementy Application:
  - `GetPlanByTripIdQueryValidator`
  - `GetPlanByTripIdQueryHandler`
  - niewielki serwis read-modelowy, np. `ITripPlanReadService` lub `IPlanReadModelMapper`
- wymagane elementy persistence/domeny:
  - `TripPlan` jako naglowek planu
  - `PlanItem` jako pozycja planu
  - opcjonalnie oddzielny mechanizm dla `tags` na itemie:
    - `text[]` / `jsonb` w `plan_items`, albo
    - osobna tabela `plan_item_tags`
- typy wspierajace:
  - `Trip`
  - `PlanStatus`
  - `IAppDbContext`
  - `ResultErrors.PlanNotFound(...)`

### Wyodrebnienie logiki do service
Sam query handler moze pozostac cienki, ale warto wydzielic wspoldzielona logike odczytu planu do malego serwisu, bo ten sam mapping bedzie potrzebny takze przy:
- `PUT /trips/{tripId}/plan`
- `POST /trips/{tripId}/plan/save`
- ewentualnym zwracaniu planu po zakonczeniu workerowego zapisu

Rekomendowany podzial:
- endpoint:
  - odczyt/generowanie `X-Correlation-Id`
  - zbudowanie requestu i wyslanie `GetPlanByTripIdQuery`
  - mapowanie `Result` do HTTP
- validator:
  - walidacja `TripId != Guid.Empty`
- handler:
  - ownership i rozroznienie `TRIP_NOT_FOUND` vs `PLAN_NOT_FOUND`
  - wywolanie serwisu read-modelowego
- `ITripPlanReadService`:
  - pobranie naglowka planu i items
  - sortowanie items
  - zlozenie `PlanQueryModel`
  - mapowanie statusu oraz pol czasowych i enumow

## 3. Szczegoly odpowiedzi
- `200 OK`
  ```json
  {
    "tripId": "uuid",
    "version": 3,
    "status": "generated|saved",
    "generatedFromJobId": "uuid|null",
    "generatedAt": "timestamp|null",
    "savedAt": "timestamp|null",
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
        "tags": ["culture", "walking"],
        "createdAt": "timestamp",
        "updatedAt": "timestamp"
      }
    ]
  }
  ```
- Naglowki odpowiedzi:
  - `X-Correlation-Id`
- Kody statusu:
  - `200` po poprawnym odczycie planu
  - `400 VALIDATION_ERROR` dla bledow route/input, np. pusty `tripId`
  - `401 UNAUTHORIZED` po wlaczeniu auth
  - `404 TRIP_NOT_FOUND` gdy trip nie istnieje, jest usuniety albo nie nalezy do usera
  - `404 PLAN_NOT_FOUND` gdy trip istnieje, ale brak planu
  - `500 INTERNAL_ERROR` dla bledow nieoczekiwanych

### Krytyczne dopasowanie schema -> DTO
Aktualny fizyczny model SQL nie pokrywa jeszcze calego `Plan DTO`. Przed implementacja endpointu trzeba domknac persistence tak, aby dalo sie bez strat odwzorowac:
- `version`
- `status`
- `generatedFromJobId`
- `generatedAt`
- `savedAt`
- `summary`
- dla itemu:
  - `dayNumber`
  - `order`
  - `title`
  - `description`
  - `locationText`
  - `startTime`
  - `endTime`
  - `durationMinutes`
  - `costLevel`
  - `tags`
  - `createdAt`
  - `updatedAt`

Najwazniejsze rozjazdy dzis:
- `trip_plans` ma `generation_job_id`, `title`, `summary`, `created_at`, `updated_at`, ale brak mu co najmniej `version`, `saved_at` i czytelnego znacznika statusu;
- `plan_items` ma obecnie `item_date`, `item_time`, `sort_order`, `place_type`, `place_name`, `description`, `created_at`, co nie wystarcza do DTO 8.1;
- `PlanItemQueryModel` przewiduje `updatedAt`, `endTime`, `durationMinutes` i `tags`, ktorych persistence dzis nie przechowuje.

Rekomendacja wdrozeniowa:
- nie implementowac endpointu na podstawie stanu "jakos zmapujemy to po drodze";
- najpierw uzgodnic i zmigrowac schema do kontraktu API;
- dopiero potem dodac handler i endpoint.

## 4. Przeplyw danych
1. Klient wywoluje `GET /trips/{tripId}/plan` z JWT i opcjonalnym `X-Correlation-Id`.
2. Minimal API:
   - odczytuje lub generuje `X-Correlation-Id`,
   - binduje `tripId` z route,
   - pobiera `userId` z kontekstu auth; obecne `DevelopmentUserId` moze pozostac tylko jako etap przejsciowy.
3. Endpoint wysyla `GetPlanByTripIdQuery` przez `IMediator`.
4. `GetPlanByTripIdQueryValidator` sprawdza `TripId != Guid.Empty`.
5. Handler wykonuje pierwszy odczyt:
   - sprawdza, czy `trip` istnieje dla `userId`,
   - filtruje rekordy z `deleted_at != null`.
6. Jesli `trip` nie istnieje, handler zwraca `Result.Fail(ResultErrors.TripNotFound(...))`.
7. Handler lub `ITripPlanReadService` wykonuje odczyt planu:
   - pobiera naglowek planu po `tripId`,
   - pobiera items w kolejnosci `dayNumber`, `order`,
   - mapuje dane persistence na `PlanQueryModel`.
8. Jesli plan nie istnieje, zwracane jest `Result.Fail(ResultErrors.PlanNotFound(...))`.
9. Handler ustala `status`:
   - `saved`, jezeli plan ma `savedAt`
   - w przeciwnym razie `generated`
10. Endpoint zwraca `200 OK` z `Plan DTO`.

### Rekomendowany sposob odczytu z bazy
Najprostsza i czytelna implementacja MVP to dwa lekkie odczyty:
- query 1: sprawdzenie ownership i istnienia `trip`
- query 2: pobranie planu naglowka i items

To jest uzasadnione, bo spec wymaga rozroznienia `TRIP_NOT_FOUND` od `PLAN_NOT_FOUND`. Jedna duza projekcja z joinami jest mozliwa, ale komplikuje obsluge dwoch roznych `404` i przy jednym planie nie daje realnej przewagi.

### Rekomendowany model persistence
Poniewaz fizyczne tabele juz istnieja, najlepsza sciezka to ich rozszerzenie i podpiecie do EF Core, a nie tworzenie konkurencyjnych tabel obok:
- naglowek:
  - mapowac do `trip_plans`
- items:
  - mapowac do `plan_items`
- jesli obecne nazwy kolumn sa zbyt stare wobec kontraktu, dodac migracje rozszerzajaca obecny model zamiast budowac nowy od zera

## 5. Wzgledy bezpieczenstwa
- Endpoint powinien byc chroniony JWT Bearer; `AllowAnonymous()` nie jest zgodne z docelowym kontraktem.
- Ownership musi byc wymuszany w handlerze na podstawie `tripId + userId`.
- Dla cudzego `tripId` odpowiedz powinna byc `404 TRIP_NOT_FOUND`, nie `403`, aby nie ujawniac istnienia zasobu.
- Endpoint nie powinien zwracac zadnych danych workerowych ani payloadow AI; tylko `Plan DTO`.
- Logi nie powinny zawierac calych itemow planu ani surowych danych wejscia generacji na poziomie `Information`.
- `X-Correlation-Id` powinien byc propagowany, aby laczyc odczyt planu z logami generacji i zapisow.
- Wszystkie operacje I/O musza byc cancelable przez `CancellationToken`.
- Przy wdrozeniu RLS lub podobnych mechanizmow DB ownership na `trip_plans` / `plan_items` powinien byc oparty o relacje do `trip.user_id`.

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - `tripId` jest pustym `Guid`
  - route parameter nie przechodzi bindowania
- `404 TRIP_NOT_FOUND`
  - trip nie istnieje
  - trip nalezy do innego usera
  - trip jest soft-deleted
- `404 PLAN_NOT_FOUND`
  - trip istnieje, ale brak naglowka planu
  - trip istnieje, ale plan nie ma jeszcze zadnych danych uznawanych za plan gotowy do odczytu
- `401 UNAUTHORIZED`
  - brak lub niepoprawny token po wlaczeniu auth
- `500 INTERNAL_ERROR`
  - niespojnosc schema i modelu EF
  - nieobslugiwany blad mapowania enumow/czasow
  - nieoczekiwany blad runtime/DB

### Rejestrowanie bledow
Ten endpoint jest read-only, wiec nie ma uzasadnienia dla osobnej tabeli bledow. Rekomendacja:
- expected failures obslugiwac przez `Result` + `ProblemDetails`
- unexpected failures przez `ExceptionHandlingMiddleware` i strukturalne logi
- nie zapisywac bledow tego endpointu do `ai_generation_job.error_code` ani do nowej tabeli, bo to nie jest blad procesu generacji

### Wymagane rozszerzenia warstwy bledow
`ResultErrors` ma juz `TripNotFound`, ale brakuje stabilnego bledu dla planu. Nalezy dodac:
- `ResultErrors.PlanNotFound(string? target = null)`
  - `Code = "PLAN_NOT_FOUND"`
  - `Status = 404`

## 7. Wydajnosc
- Uzywac `AsNoTracking()` dla calego odczytu.
- Odczyt naglowka planu wykonywac po `tripId`, korzystajac z PK/indeksu na `trip_plans.trip_id`.
- Odczyt items wykonywac po `trip_id` i w kolejnosci wspieranej indeksem.
- Jesli plan ma duzo items, pobierac tylko pola potrzebne do `PlanQueryModel`, bez materializacji pelnych grafow encji.
- Dwa selekty sa akceptowalne i czytelne; optymalizacja do jednej projekcji ma sens dopiero po potwierdzeniu realnego problemu.
- Warto utrzymac indeks sortujacy items zgodnie z rzeczywistym kontraktem odczytu:
  - jesli trzymamy `day_number` i `order`, indeks na `(trip_id, day_number, "order")`
  - jesli przejsciowo zostaje `item_date`, indeks musi wspierac stabilne mapowanie do `dayNumber`
- Endpoint bedzie naturalnie czesto odpytywany po generacji i po edycji planu, ale nie jest pollingowym hotspotem porownywalnym z job status endpointem; prostota i czytelnosc sa wazniejsze niz mikrooptymalizacje.

## 8. Kroki implementacji
1. Uzgodnic kanoniczny model planu
   - potwierdzic, ze obowiazujacy jest model strukturalny z `.ai/api_plan v2.md`
   - uznac tekstowy opis `trip_plan.current_text` z `.ai/db_plan.md` za nieaktualny dla plan endpoints
   - doprecyzowac, czy fizyczne tabele pozostaja plural (`trip_plans`, `plan_items`) i tylko dokumentacja wymaga korekty

2. Domknac schema persistence do `Plan DTO`
   - porownac obecne kolumny `trip_plans` i `plan_items` z kontraktem 8.1
   - dodac brakujace kolumny/migracje dla naglowka planu:
     - `version`
     - `saved_at`
     - ewentualny explicytny status lub zasade jego wyliczania
     - `generated_at`, jesli ma byc trzymane na planie zamiast odczytywane z `trip`
   - dodac brakujace kolumny/migracje dla itemow:
     - `day_number`
     - `order`
     - `title`
     - `location_text`
     - `start_time`
     - `end_time`
     - `duration_minutes`
     - `cost_level`
     - `updated_at`
     - `tags` albo osobne `plan_item_tags`

3. Dodac modele domenowe i EF Core
   - utworzyc encje `TripPlan` i `PlanItem`
   - dodac konfiguracje EF dla obu encji
   - rozszerzyc `IAppDbContext` i `AppDbContext` o `DbSet<TripPlan>` i `DbSet<PlanItem>`
   - zachowac mapowanie do istniejacych fizycznych tabel

4. Uporzadkowac kontrakty Application dla feature `Plans`
   - zmienic `GetPlanByTripIdQuery` na ksztalt zgodny z projektem, np. `GetPlanByTripIdQuery(Guid UserId, GetPlanByTripIdQueryRequest Request) : IRequest<Result<GetPlanByTripIdQueryResponse>>`
   - dodac `GetPlanByTripIdQueryValidator`
   - utrzymac DTO w `Features/Plans/Queries`

5. Dodac blad domenowy i mapowanie HTTP
   - dodac `ResultErrors.PlanNotFound(...)`
   - upewnic sie, ze `ResultHttpMapper` poprawnie zwraca `ProblemDetails` z kodem `PLAN_NOT_FOUND`

6. Zaimplementowac odczyt planu
   - dodac `GetPlanByTripIdQueryHandler`
   - wykonac check ownership po `tripId + userId`
   - zwrocic `TRIP_NOT_FOUND`, jesli trip nie istnieje
   - pobrac naglowek planu i items przez `AsNoTracking()`
   - zwrocic `PLAN_NOT_FOUND`, jesli brak planu
   - zmapowac persistence do `PlanQueryModel`

7. Wydzielic wspoldzielony serwis read-modelowy
   - dodac `ITripPlanReadService` lub `IPlanReadModelMapper`
   - umiescic tam logike skladania `PlanQueryModel`
   - przygotowac go do ponownego uzycia przez `PUT /trips/{tripId}/plan` i `POST /trips/{tripId}/plan/save`

8. Dodac endpoint Minimal API
   - rozszerzyc `TripsEndpoints` o `GET /{tripId:guid}/plan` albo dodac dedykowane `PlansEndpoints` pod grupa `/trips`
   - zachowac obsluge `X-Correlation-Id`
   - dodac `.Produces<PlanQueryModel>(200)`, `.ProducesProblem(400)`, `.ProducesProblem(401)`, `.ProducesProblem(404)`
   - docelowo zastapic `AllowAnonymous()` przez `RequireAuthorization()`

9. Dodac testy jednostkowe
   - validator: `Guid.Empty`
   - handler: zwraca `TRIP_NOT_FOUND`, gdy trip nie istnieje
   - handler: zwraca `TRIP_NOT_FOUND`, gdy trip nalezy do innego usera
   - handler: zwraca `PLAN_NOT_FOUND`, gdy trip istnieje bez planu
   - handler: poprawnie mapuje status, wersje, znaczniki czasu i items
   - handler: zwraca items w oczekiwanej kolejnosci

10. Dodac testy integracyjne API
   - `GET /trips/{tripId}/plan` zwraca `200` dla istniejacego planu
   - odpowiedz zawiera `X-Correlation-Id`
   - odpowiedz ma shape zgodny z `Plan DTO`
   - brak planu zwraca `404 PLAN_NOT_FOUND`
   - cudzy lub usuniety trip zwraca `404 TRIP_NOT_FOUND`
   - enumy i pola czasu serializuja sie zgodnie z kontraktem API

11. Uporzadkowac dokumentacje techniczna
   - zsynchronizowac `.ai/db_plan.md` z rzeczywistym modelem strukturalnym planu
   - dopisac, ze obecny endpoint czyta z `trip_plans` + `plan_items`
   - uniknac dalszego rozjazdu miedzy dokumentacja a rzeczywista schema i modelami Application
