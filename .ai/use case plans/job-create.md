# API Endpoint Implementation Plan: POST /trips/{tripId}/generation-jobs

## 1. Przeglad punktu koncowego
Endpoint sluzy do zakolejkowania nowego zadania generacji lub regeneracji planu dla istniejacego `trip`. Jest to endpoint typu command/write i zgodnie z kontraktem async job pattern musi zwracac `202 Accepted` natychmiast po zapisaniu rekordu joba, bez wykonywania wywolania OpenAI w pipeline HTTP.

Najwazniejsze cechy wdrozenia:
- endpoint jest podpiety pod zasob `trip`, wiec zawsze musi sprawdzac ownership i istnienie wycieczki;
- walidacja biznesowa opiera sie nie na body requestu, ale na aktualnym stanie rekordu `trip` oraz jego tagach;
- po sukcesie backend zapisuje rekord `ai_generation_job` ze statusem `queued` oraz dane potrzebne workerowi do pozniejszego przetworzenia;
- rekomendowane jest wymuszenie zasady "co najwyzej jeden aktywny job na trip" przez partial unique index i obsluge `409 JOB_ALREADY_ACTIVE`;
- aktualne repo ma juz szkice DTO `Jobs`, ale nie ma jeszcze handlera, encji joba, encji snapshotu ani endpointu dla `POST /trips/{tripId}/generation-jobs`.

Istotna niespojnosc specyfikacji, ktora trzeba rozstrzygnac w kodzie:
- spec 7.1 mowi o pojedynczym "stay length", ale obecny model `Trip` przechowuje `StayLengthMinDays` i `StayLengthMaxDays`;
- rekomendacja wdrozeniowa dla MVP: uznac, ze do kolejkowania obie wartosci musza byc obecne i kazda musi miescic sie w `[2..21]`, a dodatkowo `StayLengthMaxDays >= StayLengthMinDays`;
- to pozwala pozostac zgodnym z obecnym modelem domenowym bez wprowadzania ukrytych heurystyk na etapie queue.

Komentarz architekta: tak oba StayLengthMinDays, StayLengthMaxDays mają być uwzględnione

## 2. Szczegoly zadania
- Metoda HTTP: `POST`
- Struktura URL: `/trips/{tripId}/generation-jobs`
- Naglowki:
  - docelowo wymagane: `Authorization: Bearer <token>`
  - opcjonalne: `X-Correlation-Id`
  - rekomendowane: `Idempotency-Key`, jesli zespol chce ograniczyc duplikaty przy retry klienta
- Parametry:
  - wymagane: `tripId` (`Guid`) w route
  - opcjonalne: brak
- Request Body:
  - brak; walidacja opiera sie na zapisanym stanie `trip`
  - obecny placeholder `QueueGenerationJobCommandModel(bool? UseProfileDefaults)` nie wynika ze specyfikacji 7.1 i powinien zostac usuniety z kontraktu HTTP albo pozostac nieuzywanym artefaktem przejsciowym tylko do czasu refaktoru

### Wymagane reguly walidacji
- `tripId` nie moze byc pustym `Guid`
- `trip` musi istniec, nalezec do aktualnego usera i nie byc soft-deleted
- `DateFrom` i `DateTo` musza byc obecne
- `StayLengthMinDays` i `StayLengthMaxDays` musza byc obecne
- `StayLengthMinDays` i `StayLengthMaxDays` musza miescic sie w `[2..21]`
- `StayLengthMaxDays` musi byc `>= StayLengthMinDays`
- `PeopleCount` musi byc obecne i dodatnie
- co najmniej jeden z warunkow kontekstowych musi byc spelniony:
  - `NoteText` po trim nie jest puste
  - `PlaceText` po trim nie jest puste
  - liczba tagow tripa wynosi co najmniej `2`
- jesli obowiazuje zasada jednego aktywnego joba, brak innego joba dla tego samego `tripId` w statusie `queued` lub `processing`

### Wymagane typy i modele
- istniejace do dostosowania:
  - `QueueGenerationJobCommand`
  - `QueueGenerationJobCommandRequest`
  - `QueueGenerationJobCommandResponse`
  - `GenerationJobQueryModel`
- rekomendowane nowe lub zmienione typy Application:
  - `QueueGenerationJobCommand(Guid UserId, QueueGenerationJobCommandRequest Request) : IRequest<Result<QueueGenerationJobCommandResponse>>`
  - `QueueGenerationJobCommandRequest(Guid TripId)`
  - `QueueGenerationJobCommandRequestValidator`
  - dedykowany model odpowiedzi, np. `QueuedGenerationJobQueryModel`, jesli zespol chce zachowac zwarta odpowiedz 7.1 i nie przeciekac pol z endpointow statusowych
- nowe typy domenowe / persistence:
  - `AiGenerationJob`
  - `TripInputSnapshot`
  - opcjonalny enum lub stale dla kodow bledow workerowych
- typy istniejace, ktore beda wykorzystane pomocniczo:
  - `Trip`
  - `TripTag`
  - `GenerationJobStatus`
  - `InputSnapshotKind`

### Wyodrebnienie logiki do service
Handler nie powinien zawierac calej logiki walidacji gotowosci do generacji i budowania payloadu dla worker'a. Najlepszy podzial odpowiedzialnosci:
- `QueueGenerationJobCommandHandler` odpowiada za ownership, transakcje i zapis
- nowy serwis aplikacyjny, np. `ITripGenerationPreparationService`, odpowiada za:
  - sprawdzenie regul `GENERATION_REQUIREMENTS_NOT_MET`
  - policzenie nastepnego `generationNo` dla `trip_input_snapshot`
  - zbudowanie `requestPayload` do `ai_generation_job`
  - zbudowanie `payload` do `trip_input_snapshot`
- osobny serwis do samego uruchamiania AI nie jest potrzebny na etapie tego endpointu; wywolanie AI ma nastapic pozniej w `BackgroundService`

## 3. Szczegoly odpowiedzi
- `202 Accepted`
  ```json
  {
    "job": {
      "id": "uuid",
      "tripId": "uuid",
      "status": "queued",
      "requestedAt": "timestamp",
      "attemptNo": 0
    }
  }
  ```
- Naglowki odpowiedzi:
  - `X-Correlation-Id`
- Kody statusu:
  - `202` po poprawnym zapisaniu joba
  - `400 VALIDATION_ERROR` dla bledow technicznych requestu, np. pusty `tripId` lub niepoprawny binding
  - `400 GENERATION_REQUIREMENTS_NOT_MET` dla niespelnionych warunkow biznesowych generacji
  - `401 UNAUTHORIZED` docelowo po wlaczeniu auth
  - `404 TRIP_NOT_FOUND` gdy `tripId` nie istnieje, nalezy do innego usera albo rekord jest soft-deleted
  - `409 JOB_ALREADY_ACTIVE` gdy dla `tripId` istnieje juz aktywny job
  - `500 INTERNAL_ERROR` dla bledow nieoczekiwanych

Rekomendacja dla kontraktu odpowiedzi:
- nie zwracac pelnego `GenerationJobQueryModel`, jesli zawiera pola nieobecne w spec 7.1, takie jak `startedAt`, `finishedAt`, `discarded`, `discardReason`;
- lepiej dodac mniejszy model tylko dla operacji queue, a `GenerationJobQueryModel` zostawic dla `GET /generation-jobs/{jobId}`.

## 4. Przeplyw danych
1. Klient wywoluje `POST /trips/{tripId}/generation-jobs` z JWT i opcjonalnym `X-Correlation-Id`.
2. Minimal API w `TripsEndpoints`:
   - odczytuje lub generuje `X-Correlation-Id`,
   - binduje `tripId` z route,
   - nie binduje body, bo kontrakt 7.1 go nie przewiduje,
   - pobiera `userId` z kontekstu auth; tymczasowe `DevelopmentUserId` moze pozostac jedynie jako etap przejsciowy.
3. Endpoint wysyla `QueueGenerationJobCommand` przez `IMediator`.
4. `QueueGenerationJobCommandRequestValidator` wykonuje walidacje skladniowa:
   - `TripId != Guid.Empty`.
5. Handler laduje `Trip` po `tripId` i `userId`, razem z `TripTags`, oraz filtruje rekordy soft-deleted.
6. Jesli rekord nie istnieje, handler zwraca `Result.Fail(ResultErrors.TripNotFound(...))`.
7. `ITripGenerationPreparationService` sprawdza warunki gotowosci do generacji:
   - obecne daty,
   - obecne i poprawne wartosci stay range,
   - dodatni `PeopleCount`,
   - `NoteText` lub `PlaceText` lub co najmniej dwa tagi.
8. Gdy warunki nie sa spelnione, handler zwraca `Result.Fail(ResultErrors.GenerationRequirementsNotMet(...))`, najlepiej z `details` opisujacymi, ktore warunki zawiodly.
9. Handler sprawdza konflikt aktywnego joba:
   - najpierw lekki pre-check po `tripId` i statusach `queued|processing`,
   - nastepnie zapis jest dodatkowo chroniony przez partial unique index, aby uniknac race condition.
10. W jednej transakcji handler:
    - oblicza nastepny `generationNo`,
    - tworzy rekord `trip_input_snapshot` typu `before_generation`,
    - tworzy rekord `ai_generation_job` ze statusem `queued`, `requestedAt = UtcNow`, `attemptNo = 0`,
    - zapisuje `requestPayload`, z ktorego worker bedzie mogl wznowic przetwarzanie po restarcie aplikacji.
11. Handler mapuje encje joba do modelu odpowiedzi i zwraca `202 Accepted`.
12. Dalsze przetwarzanie odbywa sie poza requestem HTTP:
    - `BackgroundService` polluje baze po `queued` jobach,
    - aktualizuje statusy do `processing`, `succeeded`, `failed`, `canceled`,
    - zapisuje wynik lub blad do `ai_generation_job`,
    - nie uzywa `Task.Run()` z endpointu ani z handlera.

### Zawartosc `requestPayload` i snapshotu
Minimalny payload zapisany w DB powinien zawierac wszystko, co jest potrzebne workerowi po restarcie:
- `tripId`
- `userId`
- `title`
- `placeText`
- `noteText`
- `dateFrom`
- `dateTo`
- `stayLengthMinDays`
- `stayLengthMaxDays`
- `peopleCount`
- `budgetLevel`
- `pace`
- uporzadkowana liste tagow z `tagId`, `code`, `displayName`, `order`

To pozwala zachowac zgodnosc z regula "persist everything required to resume processing after restart".

## 5. Wzgledy bezpieczenstwa
- Endpoint powinien byc chroniony JWT Bearer; `AllowAnonymous()` nie jest zgodne z docelowym kontraktem.
- Ownership musi byc egzekwowany w handlerze przez filtr `tripId + userId`; odpowiedz dla cudzego zasobu powinna byc `404 TRIP_NOT_FOUND`, nie `403`.
- Nie wolno przyjmowac `userId` z body ani query.
- Rate limiting per user jest wymagany dla endpointow startujacych generacje, bo to bezposrednio kontroluje koszt integracji AI.
- `requestPayload` i `trip_input_snapshot.payload` moga zawierac dane wrazliwe biznesowo; nie nalezy ich logowac w `ILogger`.
- W logach i `ProblemDetails` nalezy ograniczyc sie do `tripId`, `jobId`, `traceId`, `correlationId`, bez dumpowania `noteText`.
- Wszystkie operacje I/O musza byc async i przyjmowac `CancellationToken`.
- W przypadku wdrozenia `Idempotency-Key` nalezy pilnowac, by nie stal sie zrodlem enumeracji cudzych jobow; klucz powinien byc powiazany z userem i sciezka endpointu.

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - `tripId` jest pustym `Guid`
  - binder nie potrafi sparsowac parametru route
- `400 GENERATION_REQUIREMENTS_NOT_MET`
  - brak `DateFrom`
  - brak `DateTo`
  - brak `StayLengthMinDays` lub `StayLengthMaxDays`
  - `StayLengthMinDays` albo `StayLengthMaxDays` poza `[2..21]`
  - `StayLengthMaxDays < StayLengthMinDays`
  - brak `PeopleCount` albo `PeopleCount <= 0`
  - `NoteText` puste po trim, `PlaceText` puste po trim i mniej niz 2 tagi
- `404 TRIP_NOT_FOUND`
  - rekord nie istnieje
  - rekord nalezy do innego usera
  - rekord jest soft-deleted
- `409 JOB_ALREADY_ACTIVE`
  - istnieje juz job w statusie `queued` lub `processing` dla tego samego `tripId`
  - rownolegly request wpadl na partial unique index podczas zapisu
- `401 UNAUTHORIZED`
  - brak lub niepoprawny token po wlaczeniu auth
- `500 INTERNAL_ERROR`
  - blad DB
  - wyjatek w serializacji payloadu
  - inny nieoczekiwany blad runtime

### Rejestrowanie bledow
Aktualny plan DB nie ma dedykowanej tabeli technicznych bledow i nie warto jej dodawac tylko dla tego endpointu. Rekomendacja:
- dla bledow przed utworzeniem joba korzystac z `Result` + `ProblemDetails` + `ILogger`
- dla bledow po zakolejkowaniu, ale podczas przetwarzania workerem, zapisywac `error_code` i `error_message` w `ai_generation_job`
- nie zapisywac osobnych rekordow `ai_generation_job` dla odrzuconych requestow typu `TRIP_NOT_FOUND` lub `GENERATION_REQUIREMENTS_NOT_MET`

### Wymagane rozszerzenia `ResultErrors`
Nalezy dodac stabilne bledy domenowe:
- `TRIP_NOT_FOUND` ze statusem `404`
- `GENERATION_REQUIREMENTS_NOT_MET` ze statusem `400`
- `JOB_ALREADY_ACTIVE` ze statusem `409`

## 7. Wydajnosc
- Ladowac `Trip` wraz z `TripTags` jednym zapytaniem, bez dodatkowych round-tripow po kazdym tagu.
- Utrzymac indeksy zgodne z `.ai/db_plan.md`:
  - `ai_job_trip_requested_idx (trip_id, requested_at DESC)`
  - `ai_job_status_requested_idx (status, requested_at DESC)`
  - `ai_job_one_active_per_trip_idx (trip_id) WHERE status IN ('queued','processing')`
  - `trip_input_snapshot_trip_gen_idx (trip_id, generation_no DESC)`
- Konflikt aktywnego joba rozwiazywac na poziomie DB, nie tylko w kodzie aplikacji.
- Tworzenie joba i snapshotu wykonywac w jednej transakcji, aby uniknac pol-zapisow.
- `requestPayload` powinien byc mozliwie zwarty; zapisywac tylko dane potrzebne workerowi, nie cale encje EF.
- Endpoint ma byc lekki obliczeniowo: bez OpenAI, bez `Task.Run()`, bez blokowania requestu oczekiwaniem na wynik generacji.
- Worker powinien pozniej stosowac bounded parallelism i retry tylko dla bledow transientnych; retry nie powinny byc czescia logiki endpointu queue.

## 8. Kroki implementacji
1. Uporzadkowac kontrakt endpointu
   - dodac route `POST /trips/{tripId:guid}/generation-jobs` do `TripsEndpoints`
   - pozostawic endpoint bez body
   - zachowac obsluge `X-Correlation-Id`
2. Uporzadkowac kontrakty Application
   - zmienic `QueueGenerationJobCommand` tak, aby implementowal `IRequest<Result<QueueGenerationJobCommandResponse>>`
   - dodac `UserId` do commandu
   - uproscic `QueueGenerationJobCommandRequest` do samego `TripId`
   - usunac albo wycofac z HTTP `QueueGenerationJobCommandModel(bool? UseProfileDefaults)`
3. Dodac walidacje i kody bledow
   - utworzyc `QueueGenerationJobCommandRequestValidator`
   - rozszerzyc `ResultErrors` o `TRIP_NOT_FOUND`, `GENERATION_REQUIREMENTS_NOT_MET`, `JOB_ALREADY_ACTIVE`
   - upewnic sie, ze `ResultHttpMapper` mapuje `409` poprawnie do `ProblemDetails`
4. Dodac modele domenowe i persistence
   - utworzyc encje `AiGenerationJob` i `TripInputSnapshot`
   - rozszerzyc `IAppDbContext` oraz `AppDbContext` o nowe `DbSet<>`
   - dodac konfiguracje EF Core, indeksy i migracje
   - jesli zespol chce zachowac pola `discarded` i `discardReason` z modeli query, uzgodnic schema drift miedzy `.ai/api_plan v2.md` a `.ai/db_plan.md`
5. Dodac serwis przygotowania generacji
   - utworzyc `ITripGenerationPreparationService`
   - zaimplementowac walidacje gotowosci tripa
   - zaimplementowac budowe `requestPayload`
   - zaimplementowac wyliczanie kolejnego `generationNo`
6. Zaimplementowac handler MediatR
   - pobrac `Trip` wraz z tagami po `tripId + userId`
   - zwrocic `TRIP_NOT_FOUND`, jesli rekord nie istnieje
   - wywolac serwis przygotowania
   - sprawdzic aktywny job
   - zapisac `TripInputSnapshot` i `AiGenerationJob` w jednej transakcji
   - zwrocic `202` z kompaktowym modelem odpowiedzi
7. Dodac endpoint Minimal API
   - rozszerzyc `TripsEndpoints`
   - dodac `.Produces<QueueGenerationJobCommandResponse>(202)`
   - dodac `.ProducesProblem(400)`, `.ProducesProblem(401)`, `.ProducesProblem(404)`, `.ProducesProblem(409)`
   - docelowo ustawic `RequireAuthorization()`
8. Przygotowac worker
   - dodac `BackgroundService` przetwarzajacy `queued` joby
   - aktualizowac statusy i `attemptNo`
   - zapisywac `error_code` i `error_message` przy porazce
   - nie wdrazac wywolan AI w samym endpointcie
9. Dodac testy
   - jednostkowe dla walidacji gotowosci generacji
   - jednostkowe dla `QueueGenerationJobCommandRequestValidator`
   - testy handlera:
     - sukces zwraca `202` i zapisuje job + snapshot
     - `TRIP_NOT_FOUND` dla cudzego, nieistniejacego lub usunietego tripa
     - `GENERATION_REQUIREMENTS_NOT_MET` dla brakujacych dat, stay range, people count i slabego kontekstu
     - `JOB_ALREADY_ACTIVE` przy istniejacym jobie `queued` lub `processing`
   - testy integracyjne endpointu:
     - `POST /trips/{tripId}/generation-jobs` zwraca `202`
     - odpowiedz zawiera `X-Correlation-Id`
     - przy rownoleglych requestach tylko jeden zapis przechodzi, drugi zwraca `409`
     - endpoint nie wykonuje zadnego wywolania AI inline
