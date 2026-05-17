# API Endpoint Implementation Plan: POST /trips/{tripId}/plan/save

## 1. Przeglad punktu koncowego
Endpoint `POST /trips/{tripId}/plan/save` oznacza aktualny plan jako zapisany przez uzytkownika i zwraca zaktualizowany stan zapisu:
- `status = saved`
- `savedAt = <timestamp>`
- `version = previousVersion + 1`

Najwazniejsze wymagania funkcjonalne:
- `409 INPUT_CHANGED_SINCE_GENERATION` gdy dane wejsciowe tripa zmienily sie od czasu generacji planu;
- `404 PLAN_NOT_FOUND` gdy brak planu dla istniejacego i dostepnego tripa;
- opcjonalny `audit_event` z `event_type = plan_saved`.

Najwazniejsze fakty o obecnym stanie repo:
- istnieja kontrakty `SavePlanCommand`, `SavePlanCommandRequest`, `SavePlanCommandResponse`;
- `TripPlan` oraz `trip_plans` maja juz model wersjonowania save (`version`, `status`, `generated_at`, `saved_at`);
- istnieje `Result` + `ProblemDetails` i `ResultHttpMapper`, wiec expected failures sa mapowane przez `Result`;
- logika fingerprintu inputu tripa zostala wydzielona do wspolnego `ITripInputFingerprintService`.

## 2. Szczegoly zadania
- Metoda HTTP: `POST`
- URL: `/trips/{tripId}/plan/save`
- Headers:
  - opcjonalne:
    - `X-Correlation-Id`
- Parametry:
  - wymagane: `tripId` (uuid) w route
  - opcjonalne: brak
- Request body: brak

### Parametry wymagane i opcjonalne
- Wymagane:
  - `tripId`
- Opcjonalne:
  - `X-Correlation-Id`

### Walidacja wejscia
- `tripId != Guid.Empty`
- `trip` musi istniec, nalezec do usera i nie byc soft-deleted (`TRIP_NOT_FOUND` dla cudzego/brakujacego)
- `plan` musi istniec (`PLAN_NOT_FOUND`)
- business rule:
  - jesli plan ma `generatedFromJobId`, to aktualny fingerprint danych wejscia tripa (trip + tagi) musi odpowiadac fingerprintowi uzytemu przy generacji;
  - przy roznicy zwrocic `INPUT_CHANGED_SINCE_GENERATION` (`409`)

### Wykorzystywane typy i modele
- Application (do dopracowania/rozszerzenia):
  - `SavePlanCommand`
  - `SavePlanCommandRequest`
  - `SavePlanCommandResponse`
  - `SavePlanResultQueryModel`
- Elementy Application:
  - `SavePlanCommandValidator`
  - `SavePlanCommandHandler`
  - `ITripInputFingerprintService` (wspoldzielony z `QueueGenerationJobCommandHandler`)
- Domain/Infrastructure:
  - `TripPlan`
  - `TripPlanConfiguration`
  - `AiGenerationJob` (odczyt `input_hash`)
  - `Trip`, `TripTag` (dane do wyliczenia fingerprintu)

### Podzial odpowiedzialnosci
- Endpoint Minimal API:
  - pobranie `tripId`,
  - ustawienie `X-Correlation-Id`,
  - wyslanie commandu przez MediatR.
- Handler:
  - ownership check,
  - rozroznienie `TRIP_NOT_FOUND` / `PLAN_NOT_FOUND`,
  - walidacja business rule `INPUT_CHANGED_SINCE_GENERATION`,
  - inkrementacja `version`,
  - ustawienie `status=saved` i `savedAt`,
  - zapis zmian.
- `ITripInputFingerprintService`:
  - budowa deterministycznego payloadu wejsciowego tripa,
  - obliczenie hash (SHA-256).

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
- `401 UNAUTHORIZED` (docelowo po wlaczeniu auth)
- `404 TRIP_NOT_FOUND` (trip nie istnieje, jest soft-deleted albo nie nalezy do usera)
- `404 PLAN_NOT_FOUND`
- `409 INPUT_CHANGED_SINCE_GENERATION`
- `500 INTERNAL_ERROR`

## 4. Przeplyw danych
1. Klient wysyla `POST /trips/{tripId}/plan/save` .
2. Endpoint Minimal API:
   - odczytuje/generuje `X-Correlation-Id`,
   - buduje `SavePlanCommand` z `UserId`, `TripId`.
3. FluentValidation waliduje command/request (`tripId`).
4. Handler sprawdza ownership tripa (`tripId + userId + deleted_at == null`).
5. Gdy trip nie istnieje/dostep zabroniony: `TRIP_NOT_FOUND`.
6. Handler laduje `trip_plan` po `tripId`.
7. Gdy plan nie istnieje: `PLAN_NOT_FOUND`.
8. Jesli `plan.GenerationJobId` istnieje:
    - pobiera `generation_jobs.input_hash` dla tego joba,
    - wylicza aktualny hash inputu tripa,
    - porownuje hashe.
9. Przy roznicy hashy: `INPUT_CHANGED_SINCE_GENERATION` (409).
10. Handler:
    - ustawia `plan.SavedAt = nowUtc`,
    - ustawia `plan.Status = saved` (lub status wyliczany z pol),
    - inkrementuje `plan.Version`,
    - opcjonalnie dodaje rekord `audit_event(plan_saved)`.
11. Zapisuje zmiany i zwraca `SavePlanCommandResponse`.
12. Endpoint mapuje wynik na `200 OK` albo `ProblemDetails`.

## 5. Wzgledy bezpieczenstwa
- Wymusic JWT (`RequireAuthorization`) dla endpointu.
- Ownership zawsze w handlerze (nie polegac na samym route param).
- Dla cudzego zasobu zwracac `404`, nie `403` (brak ujawniania istnienia tripa).
- Nie przyjmowac `userId` z body/query/header od klienta.
- Nie logowac pelnych payloadow notatek/planu; logowac tylko metadane (`tripId`, `userId`, `correlationId`, `traceId`).
- Wszystkie I/O async i cancelable (`CancellationToken`).

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - pusty `tripId`
- `404 TRIP_NOT_FOUND`
  - brak tripa
  - trip soft-deleted
  - trip innego usera
- `404 PLAN_NOT_FOUND`
  - trip istnieje, ale brak planu
- `409 INPUT_CHANGED_SINCE_GENERATION`
  - fingerprint aktualnych danych wejsciowych tripa rozni sie od fingerprintu uzytego do wygenerowania zapisywanego planu
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
- Utrzymac indeksy:
  - `trip_plans(trip_id)` (PK),
  - `generation_jobs(id)` (PK),
  - `trip_tags(trip_id, order, tag_id)` dla szybkiego wyliczenia fingerprintu.

## 8. Kroki implementacji
1. Rozszerzyc `SavePlanCommand` o `UserId` i `IRequest<Result<SavePlanCommandResponse>>`.
2. Dodac `SavePlanCommandValidator` dla `UserId` i `TripId`.
3. Dodac `ResultErrors.InputChangedSinceGeneration` (`409`).
4. Wydzielic `ITripInputFingerprintService` i przeniesc do niego logike budowy payloadu oraz SHA-256.
5. Przepiac `QueueGenerationJobCommandHandler` na wspolny serwis fingerprintu.
6. Dodac metode domenowa `TripPlan.Save(...)`.
7. Dodac `SavePlanCommandHandler`:
   - ownership check,
   - `TRIP_NOT_FOUND` / `PLAN_NOT_FOUND`,
   - porownanie aktualnego fingerprintu z `generation_jobs.input_hash`,
   - zapis planu jako `saved`.
8. Uproscic `SavePlanCommandRequest` do samego `TripId`.
9. Dodac endpoint Minimal API:
   - `MapPost("/{tripId:guid}/plan/save", ...)` w `TripsEndpoints`,
   - `.Produces<SavePlanResultQueryModel>(200)`,
   - `.ProducesProblem(400|401|404|409)`.
10. Dodac testy jednostkowe:
    - validator,
    - handler: sukces, `TRIP_NOT_FOUND`, `PLAN_NOT_FOUND`, `INPUT_CHANGED_SINCE_GENERATION`,
    - fingerprint service.
11. Dodac testy integracyjne API:
    - `200` i poprawny payload,
    - `404` trip/plan not found,
    - `409` przy zmienionym input.
12. (Opcjonalnie) Dodac `audit_event plan_saved`.
13. Zweryfikowac zgodnosc z zasadami projektu:
    - Minimal API, MediatR, FluentValidation, Result/ProblemDetails, async/cancel, ownership.
