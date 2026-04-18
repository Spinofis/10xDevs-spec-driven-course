# API Endpoint Implementation Plan: PUT /trips/{tripId}/plan

## 1. Przeglad punktu koncowego
Endpoint sluzy do pelnej, manualnej podmiany istniejacego planu wycieczki dla wskazanego `tripId`.
To endpoint write/command i po sukcesie zwraca `200 OK` z kompletnym `Plan DTO`.

Kluczowe wymagania kontraktowe:
- endpoint musi rozrozniac `404 TRIP_NOT_FOUND` i `404 PLAN_NOT_FOUND`;
- endpoint dziala jako "replace entire plan" (naglowek + cala lista itemow);
- walidacja wejsciowa musi wymusic m.in. `items[].title` required, `dayNumber >= 1`, `order` required, `startTime` w formacie `HH:mm` (gdy podane);
- status po manualnej edycji powinien byc `saved`, a plan powinien dostac nowy `version`.

Uwagi do obecnego stanu repo:
- istnieja szkielety `UpdatePlanCommand` i modele w `Features/Plans`, ale brak handlera write i brak endpointu `PUT /trips/{tripId}/plan`;
- obecny model persistence planu (`trip_plans` + `plan_items`) nie pokrywa pelnego kontraktu 8.3 (brakuje m.in. czesci pol i semantyki wersjonowania);
- dokument `.ai/db_plan.md` ma fragment historyczny o `trip_plan.current_text`; dla endpointow planu source of truth powinno pozostac `trip_plans + plan_items`.

## 2. Szczegoly zadania
- Metoda HTTP: `PUT`
- URL: `/trips/{tripId}/plan`
- Headers:
  - wymagany docelowo: `Authorization: Bearer <token>`
  - opcjonalny: `X-Correlation-Id`
- Parametry route:
  - wymagane: `tripId: uuid`

### Request body
```json
{
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
      "createdAt": "timestamp",
      "updatedAt": "timestamp",
      "placeType": "Attraction|Restaurant|Hotel"
    }
  ]
}
```

### Parametry wymagane i opcjonalne
- Wymagane:
  - `tripId`
  - `items`
  - `items[].id`
  - `items[].dayNumber`
  - `items[].order`
  - `items[].title`
  - `items[].createdAt`
  - `items[].updatedAt`
  - `items[].placeType`
- Opcjonalne:
  - `summary`
  - `items[].description`
  - `items[].locationText`
  - `items[].startTime`

### Wymagane walidacje
- `tripId != Guid.Empty`
- `items` nie moze byc `null`
- `items[].title` niepuste po `Trim()`
- `items[].dayNumber >= 1`
- `items[].order` obecne i liczba calkowita
- `items[].startTime` zgodne z `HH:mm` (gdy podane)
- `items[].placeType` nalezy do `Attraction|Restaurant|Hotel`
- `items[].id` musi byc poprawnym UUID i unikalne w ramach requestu

Uwagi implementacyjne do walidacji:
- spec bledow wspomina `endTime`, ale model requestu 8.3 nie zawiera `endTime`; walidator nalezy przygotowac pod obecny kontrakt (`startTime`) i pozostawic miejsce na latwe rozszerzenie;
- `createdAt/updatedAt` sa polami wejscia, ale wartosci persistence powinny byc traktowane jako server-authoritative (ochrona przed manipulacja timestampami klienta).

### Wykorzystywane typy i modele
- Application (Plans):
  - `UpdatePlanCommand`
  - `UpdatePlanCommandRequest`
  - `UpdatePlanCommandResponse`
  - `UpdatePlanCommandModel`
  - `PlanItemCommandModel`
  - `GetPlanByTripIdQueryResponse` + `PlanQueryModel` (do ksztaltu odpowiedzi)
- Nowe elementy do dodania:
  - `UpdatePlanCommandValidator`
  - `UpdatePlanCommandRequestValidator`
  - `UpdatePlanCommandModelValidator`
  - `PlanItemCommandModelValidator`
  - `UpdatePlanCommandHandler`
  - `ITripPlanWriteService` + implementacja
- Domain/Infrastructure:
  - aktualizacja `TripPlan` i `PlanItem` pod pola kontraktu 8.3
  - aktualizacja konfiguracji EF (`TripPlanConfiguration`, `PlanItemConfiguration`)
  - migracja SQL dla `trip_plans` i `plan_items`

### Wyodrebnienie logiki do service
Rekomendowany podzial:
- Minimal API endpoint:
  - binding route/body
  - korelacja (`X-Correlation-Id`)
  - wyslanie commandu przez MediatR
  - mapowanie `Result` do HTTP
- Validator:
  - walidacje strukturalne i formatowe
- Handler:
  - ownership check (`tripId + userId`)
  - rozroznienie `TRIP_NOT_FOUND` vs `PLAN_NOT_FOUND`
  - transakcja zapisu
  - delegowanie replace do serwisu write
- `ITripPlanWriteService`:
  - normalizacja danych
  - replace itemow
  - aktualizacja wersji/statusu/savedAt
  - zwrot modelu gotowego do odpowiedzi

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
      "createdAt": "timestamp",
      "updatedAt": "timestamp",
      "placeType": "Attraction|Restaurant|Hotel"
    }
  ]
}
```

Zasady mapowania odpowiedzi:
- po manualnym `PUT` status powinien byc `saved`;
- `version` powinien byc inkrementowany atomowo (`+1`);
- `generatedFromJobId` i `generatedAt` powinny zostac zachowane z poprzedniego planu;
- `savedAt` ustawiane na czas operacji w UTC;
- items zwracane w stabilnej kolejnosci (`dayNumber`, potem `order`, potem `id`);
- serializacja czasu `startTime` musi dawac `HH:mm`.

Kody statusu:
- `200` sukces
- `400 VALIDATION_ERROR`
- `401 UNAUTHORIZED`
- `404 TRIP_NOT_FOUND`
- `404 PLAN_NOT_FOUND`
- `500 INTERNAL_ERROR`

## 4. Przeplyw danych
1. Klient wysyla `PUT /trips/{tripId}/plan` z kompletnym body planu.
2. Minimal API w `TripsEndpoints`:
   - odczytuje/generuje `X-Correlation-Id`,
   - binduje `tripId`,
   - binduje body do `UpdatePlanCommandRequest`,
   - przekazuje `UserId` (docelowo z JWT, tymczasowo moze byc `DevelopmentUserId`).
3. Endpoint wysyla `UpdatePlanCommand` przez `IMediator`.
4. FluentValidation uruchamia walidacje commandu i zagniezdzonych modeli.
5. Handler sprawdza istnienie i ownership `trip` (`tripId`, `userId`, `deleted_at == null`).
6. Gdy brak tripa: `Result.Fail(TRIP_NOT_FOUND)`.
7. Handler laduje naglowek planu; gdy brak planu: `Result.Fail(PLAN_NOT_FOUND)`.
8. Handler rozpoczyna transakcje DB i deleguje logike replace do `ITripPlanWriteService`.
9. Serwis write:
   - normalizuje pola tekstowe,
   - mapuje itemy z requestu,
   - podmienia cala kolekcje itemow atomowo,
   - aktualizuje naglowek planu (`summary`, `status`, `version`, `savedAt`, `updatedAt`).
10. Handler zapisuje zmiany `SaveChangesAsync(cancellationToken)` i konczy transakcje.
11. Handler zwraca `UpdatePlanCommandResponse` z `PlanQueryModel`.
12. Endpoint zwraca `200 OK`.

Rekomendacja modelu danych dla 8.3:
- utrzymac `trip_plans` jako naglowek planu (1:1 z `trip`);
- utrzymac `plan_items` jako lista pozycji (1:N z `trip`);
- rozszerzyc schemat o pola niezbedne dla kontraktu PUT/GET:
  - naglowek: `version`, `status`, `generated_from_job_id`, `generated_at`, `saved_at`, `summary`, `updated_at`
  - item: `id`, `trip_id`, `day_number`, `sort_order` (mapowane na `order`), `title`, `description`, `location_text`, `start_time`, `place_type`, `created_at`, `updated_at`

## 5. Wzgledy bezpieczenstwa
- Endpoint powinien byc chroniony JWT Bearer (docelowo).
- Ownership check obowiazkowo w handlerze, nie tylko na poziomie endpointu.
- Dla obcego zasobu zwracac `404 TRIP_NOT_FOUND` (brak ujawniania istnienia cudzych rekordow).
- Nie przyjmowac `userId` z requestu.
- Nie ufac `createdAt/updatedAt` z klienta jako zrodlu prawdy; timestampy finalnie ustala serwer.
- Ograniczyc maksymalna liczbe itemow i dlugosc pol tekstowych (ochrona przed naduzyciem payloadu).
- Logowac metadane (`tripId`, `userId`, `correlationId`, `traceId`), bez pelnych tresci planu.
- Wszystkie I/O async i cancelowalne (`CancellationToken`).

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - pusty `tripId`
  - `items` brak/null
  - puste `items[].title`
  - `dayNumber < 1`
  - brak `order`
  - niepoprawny `startTime`
  - niepoprawny `placeType`
  - duplikaty `items[].id`
  - niepoprawny format JSON
- `404 TRIP_NOT_FOUND`
  - trip nie istnieje, jest soft-deleted lub nie nalezy do usera
- `404 PLAN_NOT_FOUND`
  - trip istnieje, ale plan nie istnieje
- `401 UNAUTHORIZED`
  - brak/niepoprawny token (po wlaczeniu auth)
- `500 INTERNAL_ERROR`
  - nieoczekiwany blad runtime/DB

Rejestrowanie bledow:
- ten endpoint jest synchroniczny i nie jest czescia workflow joba AI;
- nie zapisujemy tych bledow do `ai_generation_job.error_code/error_message`;
- expected failures obslugiwac przez `Result` + `ProblemDetails`;
- unexpected failures przez middleware + `ILogger`;
- opcjonalnie rejestrowac biznesowe zdarzenie "plan_updated_manually" w `audit_event` (jesli tabela uzywana).

## 7. Wydajnosc
- Replace planu wykonac w jednej transakcji.
- Unikac N+1; ladowac tylko potrzebne dane.
- Utrzymac indeksy:
  - `trip_plans(trip_id)` (PK)
  - `plan_items(trip_id, day_number, sort_order, id)` dla szybkiego odczytu i sortowania
- Dla duzych planow preferowac operacje set-based przy replace itemow.
- Wlaczyc optimistic concurrency na naglowku planu (np. token/rowversion) lub wersjonowanie atomowe oparte o `version`.
- Utrzymac serializacje `HH:mm` bez kosztownych konwersji ad hoc w wielu miejscach (wspolne konwertery JSON).

## 8. Kroki implementacji
1. Ujednolicic kontrakt feature `Plans` pod spec 8.3
   - dopasowac `UpdatePlanCommand*` i `PlanItemCommandModel` do pol z endpointu PUT.
2. Dodac walidatory FluentValidation
   - command + request + model + item model.
3. Dodac `UpdatePlanCommandHandler`
   - `IRequest<Result<UpdatePlanCommandResponse>>`
   - ownership check
   - `TRIP_NOT_FOUND` i `PLAN_NOT_FOUND`.
4. Wydzielic logike write do `ITripPlanWriteService`
   - replace naglowka i itemow
   - inkrementacja `version`
   - ustawienie `status/savedAt`.
5. Zaktualizowac warstwe domeny i EF
   - encje `TripPlan`, `PlanItem`
   - konfiguracje EF
   - `IAppDbContext` i `AppDbContext` jesli potrzebne.
6. Dodac migracje SQL
   - rozszerzyc `trip_plans` i `plan_items` do pelnego kontraktu PUT/GET.
7. Dodac endpoint Minimal API
   - `group.MapPut("/{tripId:guid}/plan", UpdatePlan)`
   - mapowanie `Result` do `200/400/401/404/500`
   - obsluga `X-Correlation-Id`.
8. Dodac/uzupelnic mapowanie odpowiedzi
   - zwracac `PlanQueryModel` zgodny z kontraktem API.
9. Dodac testy jednostkowe
   - walidatory
   - handler (`TRIP_NOT_FOUND`, `PLAN_NOT_FOUND`, sukces).
10. Dodac testy integracyjne API
   - `PUT` sukces `200`
   - walidacje `400`
   - `404 TRIP_NOT_FOUND`
   - `404 PLAN_NOT_FOUND`
   - odpowiedz zgodna ze schema (w tym format `HH:mm`).
11. Zweryfikowac zgodnosc z regualmi backendu
   - Minimal API, MediatR, Result Pattern, ProblemDetails, async + cancellation, ownership.
