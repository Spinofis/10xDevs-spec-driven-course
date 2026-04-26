# API Endpoint Implementation Plan: PUT /trips/{tripId}/plan

## 1. Przeglad punktu koncowego
Endpoint `PUT /trips/{tripId}/plan` sluzy do pelnej, manualnej podmiany istniejacego planu wycieczki.
Operacja jest typu write i po sukcesie zwraca `200 OK` z aktualnym `Plan DTO`.

Najwazniejsze wymagania:
- rozroznienie dwoch bledow `404`: `TRIP_NOT_FOUND` i `PLAN_NOT_FOUND`;
- atomowa podmiana naglowka planu oraz calej listy `items`;
- walidacja zgodna ze spec (`items[].title` required, `order` required, czas `HH:mm`);
- wynik po edycji manualnej powinien miec status `saved` i podbita wersje planu.

## 2. Szczegoly zadania
- Metoda HTTP: `PUT`
- URL: `/trips/{tripId}/plan`
- Headers:
  - wymagany docelowo: `Authorization: Bearer <token>`
  - opcjonalny: `X-Correlation-Id`
- Parametry:
  - wymagane: `tripId` (uuid) w route

### Request body
```json
{
  "summary": "string|null",
  "items": [
    {
      "id": "uuid",
      "dayNumber": 1,
      "itemDate": "datetime",
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
  - `itemDate[].itemDate`,
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

### Walidacja wejscia
- `tripId != Guid.Empty`
- `items` nie moze byc `null` i nie moze byc puste
- `items[].title` wymagane i niepuste po `Trim()`
- `items[].dayNumber >= 1`
- `itemDate[].itemDate` wymagane
- `items[].order` wymagane
- `items[].startTime` poprawne `HH:mm` (jesli podane)
- `items[].placeType` nalezy do `Attraction|Restaurant|Hotel`
- `items[].id` poprawny UUID i bez duplikatow w request
- `createdAt <= updatedAt` dla kazdego itemu

### Wykorzystywane typy i modele
- Application:
  - `UpdatePlanCommand`
  - `UpdatePlanCommandRequest`
  - `UpdatePlanCommandResponse`
  - `UpdatePlanCommandModel`
  - `PlanItemCommandModel`
  - `PlanQueryModel`
  - `PlanItemQueryModel`
- Walidatory:
  - `UpdatePlanCommandValidator`
  - `UpdatePlanCommandRequestValidator`
  - `UpdatePlanCommandModelValidator`
  - `PlanItemCommandModelValidator`
- Domain/Infrastructure:
  - `TripPlan`
  - `PlanItem`
  - `TripPlanConfiguration`
  - `PlanItemConfiguration`
  - migracja SQL dla `trip_plans` i `plan_items`

### Wyodrebnienie logiki do service
Rekomendowany podzial:
- Endpoint Minimal API: binding + correlation ID + wywolanie MediatR;
- Handler: ownership check, rozroznienie `TRIP_NOT_FOUND`/`PLAN_NOT_FOUND`, transakcja;
  - normalizacja danych,
  - podmiana itemow,
  - aktualizacja wersji i statusu,
  - mapowanie do modelu odpowiedzi.

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
      "itemDate": "datetime",
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

Kody statusu:
- `200` sukces
- `400` `VALIDATION_ERROR`
- `404` `TRIP_NOT_FOUND`
- `404` `PLAN_NOT_FOUND`
- `500` internal error

## 4. Przeplyw danych
1. Klient wysyla `PUT /trips/{tripId}/plan` z pelnym body planu.
2. Endpoint Minimal API:
   - czyta/generuje `X-Correlation-Id`,
   - binduje `tripId` i body,
   - przekazuje `UserId` do commandu 
3. MediatR uruchamia walidatory FluentValidation.
4. Handler sprawdza `trip` po `tripId + userId` oraz `deleted_at == null`.
5. Gdy trip nie istnieje: `TRIP_NOT_FOUND`.
6. Handler laduje plan (`trip_plans`), a gdy brak: `PLAN_NOT_FOUND`.
7. Handler :
   - aktualizuje naglowek (`summary`, `version`, `status`, `savedAt`, `updatedAt`),
   - usuwa stare itemy,
   - wstawia nowe itemy z requestu.
8. Zapis odbywa sie w jednej transakcji.
9. Handler zwraca `UpdatePlanCommandResponse` z `PlanQueryModel`.
10. Endpoint oddaje `200 OK`.

## 5. Wzgledy bezpieczenstwa
- Wymuszac ownership po stronie handlera, nie tylko w endpoint.
- Dla cudzego zasobu zwracac `404` zamiast `403` (brak ujawniania zasobow).
- Nie przyjmowac `userId` z request body/query.
- Ograniczyc maksymalna liczbe itemow i dlugosc pol tekstowych.
- Logowac metadane (`tripId`, `userId`, `correlationId`), nie pelna tresc planu.
- Wszystkie operacje I/O async + `CancellationToken`.

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - pusty `tripId`
  - `items` null/puste
  - `items[].title` puste
- `itemDate[].itemDate` wymagane
  - brak `items[].order`
  - niepoprawny `startTime`
  - niepoprawny `placeType`
  - duplikaty `items[].id`
- `404 TRIP_NOT_FOUND`
  - brak tripa, cudzy trip, trip soft-deleted
- `404 PLAN_NOT_FOUND`
  - trip istnieje, ale brak planu
- `500 INTERNAL_ERROR`
  - nieoczekiwany wyjatek runtime/DB

Rejestrowanie bledow:
- expected failures: `Result` + `ProblemDetails`;
- unexpected failures: middleware + `ILogger`;
- brak potrzeby zapisu bledow tego endpointu do tabeli jobow AI;
- opcjonalnie mozna dodac wpis audytowy `plan_updated_manually` do `audit_event`.

## 7. Wydajnosc
- Operacje replace wykonywac w pojedynczej transakcji.
- Unikac N+1 i pobierac tylko potrzebne dane.
- Utrzymac indeksy:
  - `trip_plans(trip_id)` (PK)
  - `plan_items(trip_id, day_number, sort_order, id)` dla stabilnego sortowania
- Przy duzej liczbie itemow preferowac operacje set-based (`delete + bulk insert` w jednym save).
- Zaimplementowac optimistic concurrency (np. `version`) dla ochrony przed utrata rownoleglych zmian.

## 8. Kroki implementacji
1. Dopasowac kontrakt `UpdatePlan*` do spec 8.3.
2. Dodac komplet walidatorow FluentValidation dla commandu i itemow.
3. Zaimplementowac `UpdatePlanCommandHandler` (`Result` pattern, ownership, 404 split).
4. Dodac `ITripPlanWriteService` i przeniesc tam logike replace planu.
5. Rozszerzyc encje i konfiguracje EF (`TripPlan`, `PlanItem`) do pelnego kontraktu.
6. Przygotowac migracje SQL dla `trip_plans` i `plan_items`.
7. Dodac endpoint Minimal API `MapPut("/{tripId:guid}/plan", ...)`.
8. Dodac mapowanie odpowiedzi do `PlanQueryModel`.
9. Dodac testy jednostkowe:
   - walidatory,
   - handler (success, `TRIP_NOT_FOUND`, `PLAN_NOT_FOUND`).
10. Dodac testy integracyjne endpointu PUT:
   - `200` sukces,
   - `400` walidacja,
   - `404` trip/plan not found,
   - zgodnosc serializacji `HH:mm`.
11. Zweryfikowac zgodnosc z zasadami projektu:
   - Minimal API, MediatR, FluentValidation, Result/ProblemDetails, async/cancel, ownership.
