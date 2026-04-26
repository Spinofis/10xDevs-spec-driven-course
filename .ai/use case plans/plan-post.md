# API Endpoint Implementation Plan: POST /trips/{tripId}/plan/save

## 1. Przeglad punktu koncowego
Endpoint `POST /trips/{tripId}/plan/save` oznacza aktualny plan jako zapisany przez uzytkownika i zwraca zaktualizowany stan zapisu:
- `status = saved`
- `savedAt = <timestamp>`
- `version = previousVersion + 1`

Najwazniejsze wymagania funkcjonalne:
- optimistic concurrency przez naglowek `If-Match` (wersja planu od klienta);
- `412 PRECONDITION_FAILED` przy niedopasowaniu wersji;
- `409 INPUT_CHANGED_SINCE_GENERATION` gdy dane wejsciowe tripa zmienily sie od czasu generacji planu;
- `404 PLAN_NOT_FOUND` gdy brak planu dla istniejacego i dostepnego tripa;
- opcjonalny `audit_event` z `event_type = plan_saved`.

Najwazniejsze fakty o obecnym stanie repo (do uwzglednienia przy wdrozeniu):
- istnieja kontrakty `SavePlanCommand`, `SavePlanCommandRequest`, `SavePlanCommandResponse`, ale brak handlera i endpointu Minimal API;
- `trip_plans`/`TripPlan` nie maja jeszcze pelnego modelu wersjonowania save (`version`, `saved_at`);
- brak wspolnej logiki parsowania `If-Match` i brak mapowania bledu `PRECONDITION_FAILED`;
- istnieje `Result` + `ProblemDetails` i `ResultHttpMapper`, wiec expected failures powinny byc mapowane przez `Result`.

## 2. Szczegoly zadania
- Metoda HTTP: `POST`
- URL: `/trips/{tripId}/plan/save`
- Headers:
  - wymagane:
    - `Authorization: Bearer <token>` (docelowo)
    - `If-Match: "3"` (cudzyslow + dodatnia liczba calkowita)
  - opcjonalne:
    - `X-Correlation-Id`
- Parametry:
  - wymagane: `tripId` (uuid) w route
  - opcjonalne: brak
- Request body: brak

### Parametry wymagane i opcjonalne
- Wymagane:
  - `tripId`
  - `If-Match`
- Opcjonalne:
  - `X-Correlation-Id`

### Walidacja wejscia
- `tripId != Guid.Empty`
- `If-Match` musi byc obecny
- `If-Match` musi miec format `"N"` (quoted integer), gdzie `N > 0`
- `trip` musi istniec, nalezec do usera i nie byc soft-deleted (`TRIP_NOT_FOUND` dla cudzego/brakujacego)
- `plan` musi istniec (`PLAN_NOT_FOUND`)
- `If-Match` musi byc zgodny z aktualna `plan.version`, inaczej `PRECONDITION_FAILED`
- business rule:
  - jesli plan ma `generatedFromJobId`, to aktualny fingerprint danych wejscia tripa (trip + tagi) musi odpowiadac fingerprintowi uzytemu przy generacji;
  - przy roznicy zwrocic `INPUT_CHANGED_SINCE_GENERATION` (`409`)

### Wykorzystywane typy i modele
- Application (do dopracowania/rozszerzenia):
  - `SavePlanCommand`
  - `SavePlanCommandRequest`
  - `SavePlanCommandResponse`
  - `SavePlanResultQueryModel`
- Rekomendowane nowe elementy Application:
  - `SavePlanCommandValidator`
  - `SavePlanCommandHandler`
  - parser naglowka, np. `IfMatchHeaderParser` (helper)
  - serwis write, np. `ITripPlanSaveService`
  - serwis fingerprintu inputu, np. `ITripInputFingerprintService` (do reuzycia z `QueueGenerationJobCommandHandler`)
- Domain/Infrastructure:
  - `TripPlan` (rozszerzenie o `Version`, `SavedAt`, opcjonalnie `GeneratedAt`)
  - `TripPlanConfiguration` (mapowanie nowych kolumn + concurrency)
  - `AiGenerationJob` (odczyt `input_hash`)
  - `Trip`, `TripTag` (dane do wyliczenia fingerprintu)

### Wyodrebnienie logiki do service
Rekomendowany podzial:
- Endpoint Minimal API:
  - pobranie `tripId`,
  - odczyt/parsing `If-Match`,
  - ustawienie `X-Correlation-Id`,
  - wyslanie commandu przez MediatR.
- Handler:
  - ownership check,
  - rozroznienie `TRIP_NOT_FOUND` / `PLAN_NOT_FOUND`,
  - orkiestracja zapisu i mapowanie `Result`.
- `ITripPlanSaveService`:
  - walidacja business rule `INPUT_CHANGED_SINCE_GENERATION`,
  - inkrementacja `version`,
  - ustawienie `status=saved` i `savedAt`,
  - atomowy zapis (opcjonalnie wraz z `audit_event`).
- `ITripInputFingerprintService`:
  - budowa deterministycznego payloadu wejsciowego tripa,
  - obliczenie hash (SHA-256),
  - porownanie z `generation_jobs.input_hash`.

## 3. Szczegoly odpowiedzi
- `200 OK`
```json
{
  "tripId": "uuid",
  "status": "saved",
  "savedAt": "timestamp",
  "version": 4
}
```

Kody statusu:
- `200` sukces
- `400 VALIDATION_ERROR` (np. brak/niepoprawny format `If-Match`, pusty `tripId`)
- `401 UNAUTHORIZED` (docelowo po wlaczeniu auth)
- `404 TRIP_NOT_FOUND` (trip nie istnieje, jest soft-deleted albo nie nalezy do usera)
- `404 PLAN_NOT_FOUND`
- `409 INPUT_CHANGED_SINCE_GENERATION`
- `412 PRECONDITION_FAILED` (niedopasowana wersja `If-Match`)
- `500 INTERNAL_ERROR`

## 4. Przeplyw danych
1. Klient wysyla `POST /trips/{tripId}/plan/save` z `If-Match`.
2. Endpoint Minimal API:
   - odczytuje/generuje `X-Correlation-Id`,
   - parsuje `If-Match` do `int ifMatchVersion`,
   - buduje `SavePlanCommand` z `UserId`, `TripId`, `IfMatchVersion`.
3. FluentValidation waliduje command/request (`tripId`, `ifMatchVersion`).
4. Handler sprawdza ownership tripa (`tripId + userId + deleted_at == null`).
5. Gdy trip nie istnieje/dostep zabroniony: `TRIP_NOT_FOUND`.
6. Handler laduje `trip_plan` po `tripId`.
7. Gdy plan nie istnieje: `PLAN_NOT_FOUND`.
8. Handler/serwis porownuje `ifMatchVersion` z `plan.Version`.
9. Przy mismatch: `PRECONDITION_FAILED` (412).
10. Jesli `plan.GeneratedFromJobId` istnieje:
    - pobiera `generation_jobs.input_hash` dla tego joba,
    - wylicza aktualny hash inputu tripa,
    - porownuje hashe.
11. Przy roznicy hashy: `INPUT_CHANGED_SINCE_GENERATION` (409).
12. W jednej transakcji:
    - ustawia `plan.SavedAt = nowUtc`,
    - ustawia `plan.Status = saved` (lub status wyliczany z pol),
    - inkrementuje `plan.Version`,
    - opcjonalnie dodaje rekord `audit_event(plan_saved)`.
13. Zapisuje zmiany i zwraca `SavePlanCommandResponse`.
14. Endpoint mapuje wynik na `200 OK` albo `ProblemDetails`.

## 5. Wzgledy bezpieczenstwa
- Wymusic JWT (`RequireAuthorization`) dla endpointu.
- Ownership zawsze w handlerze (nie polegac na samym route param).
- Dla cudzego zasobu zwracac `404`, nie `403` (brak ujawniania istnienia tripa).
- Nie przyjmowac `userId` z body/query/header od klienta.
- Walidowac i limitowac format `If-Match` (ochrona przed malformed input).
- Nie logowac pelnych payloadow notatek/planu; logowac tylko metadane (`tripId`, `userId`, `correlationId`, `traceId`).
- Wszystkie I/O async i cancelable (`CancellationToken`).

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - pusty `tripId`
  - brak `If-Match`
  - `If-Match` w zlym formacie (np. bez cudzyslowu, nie-liczba, liczba <= 0)
- `404 TRIP_NOT_FOUND`
  - brak tripa
  - trip soft-deleted
  - trip innego usera
- `404 PLAN_NOT_FOUND`
  - trip istnieje, ale brak planu
- `409 INPUT_CHANGED_SINCE_GENERATION`
  - fingerprint aktualnych danych wejsciowych tripa rozni sie od fingerprintu uzytego do wygenerowania zapisywanego planu
- `412 PRECONDITION_FAILED`
  - `If-Match` nie zgadza sie z aktualnym `plan.Version`
- `500 INTERNAL_ERROR`
  - blad DB/transakcji
  - nieoczekiwany blad runtime

Rejestrowanie bledow:
- aktualny projekt nie ma dedykowanej tabeli bledow technicznych; expected failures obslugiwac przez `Result` + `ProblemDetails`;
- unexpected failures logowac przez `ExceptionHandlingMiddleware` + `ILogger`;
- `ai_generation_job.error_code/error_message` nie jest miejscem na bledy tego endpointu (to pole workerowe);
- `audit_event` (jesli wdrozone) sluzy do zdarzen biznesowych (`plan_saved`), nie do logowania exceptionow.

## 7. Wydajnosc
- Operacja jest lekka: 1 odczyt `trip`, 1 odczyt `trip_plan`, opcjonalnie 1 odczyt joba + tagow do hash.
- Nie ladowac `plan_items` dla samego save.
- Uzywac `AsNoTracking()` dla odczytow walidacyjnych, gdzie nie sa potrzebne trackowane encje.
- Zabezpieczyc aktualizacje wersji przed race condition:
  - preferowane `UPDATE ... WHERE trip_id = @id AND version = @ifMatch`,
  - lub EF concurrency token (`xmin`) + mapowanie `DbUpdateConcurrencyException` na `412`.
- Utrzymac indeksy:
  - `trip_plans(trip_id)` (PK),
  - `generation_jobs(id)` (PK),
  - `trip_tags(trip_id, order, tag_id)` dla szybkiego wyliczenia fingerprintu.

## 8. Kroki implementacji
1. Rozszerzyc kontrakt commandu save:
   - `SavePlanCommand(Guid UserId, SavePlanCommandRequest Request) : IRequest<Result<SavePlanCommandResponse>>`.
2. Dodac parser `If-Match` (helper) i jednoznaczne mapowanie bledow parsowania na `VALIDATION_ERROR`.
3. Dodac `SavePlanCommandValidator` + walidator requestu (`TripId`, `IfMatchVersion`).
4. Dodac `SavePlanCommandHandler` z rozroznieniem `TRIP_NOT_FOUND` i `PLAN_NOT_FOUND`.
5. Dodac/rozszerzyc `ResultErrors` o:
   - `PRECONDITION_FAILED` (412),
   - `INPUT_CHANGED_SINCE_GENERATION` (409).
6. Rozszerzyc `TripPlan` i mapowanie EF:
   - `Version` (int),
   - `SavedAt` (timestamp nullable),
   - opcjonalnie `GeneratedAt` dla czytelnej semantyki statusu.
7. Przygotowac migracje SQL dla `trip_plans`:
   - dodanie kolumn,
   - backfill `version` dla rekordow istniejacych (np. `1`),
   - indeksy, jesli potrzebne.
8. Wydzielic `ITripInputFingerprintService` i przeniesc/reuzyc logike hashowania z `QueueGenerationJobCommandHandler`.
9. Dodac `ITripPlanSaveService` (transakcja + business rule + inkrementacja wersji).
10. Dodac endpoint Minimal API:
    - `MapPost("/{tripId:guid}/plan/save", ...)` w `TripsEndpoints`,
    - odczyt `If-Match` z naglowka,
    - `.Produces<SavePlanResultQueryModel>(200)`,
    - `.ProducesProblem(400|401|404|409|412)`.
11. Dodac testy jednostkowe:
    - walidatory commandu i parser `If-Match`,
    - handler: sukces, `TRIP_NOT_FOUND`, `PLAN_NOT_FOUND`, `PRECONDITION_FAILED`, `INPUT_CHANGED_SINCE_GENERATION`.
12. Dodac testy integracyjne API:
    - `200` i poprawny payload,
    - `400` dla zlego `If-Match`,
    - `404` trip/plan not found,
    - `409` przy zmienionym input,
    - `412` przy stalej wersji.
13. (Opcjonalnie) Dodac `audit_event plan_saved` i test, ze event powstaje atomowo z zapisem planu.
14. Zweryfikowac zgodnosc z zasadami projektu:
    - Minimal API, MediatR, FluentValidation, Result/ProblemDetails, async/cancel, ownership.
