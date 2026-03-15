# Background Worker Implementation Plan: AiGenerationJob Processor

## 1. Przeglad workera
Worker odpowiada za przetwarzanie rekordow `generation_jobs` / `AiGenerationJob` poza pipeline HTTP. Endpoint `POST /trips/{tripId}/generation-jobs` ma tylko zapisac job do bazy i zwrocic `202 Accepted`; cala logika kosztowna i zawodna operacyjnie musi zostac przeniesiona do procesu background.

Najwazniejsze zalozenia dla MVP:
- source of truth dla kolejki pozostaje PostgreSQL;
- worker nigdy nie czyta "aktualnego" stanu `Trip`, tylko korzysta z payloadu zapisanego przy kolejkowaniu joba;
- przetwarzanie musi byc odporne na restart hosta, rownolegle instancje i chwilowe bledy OpenAI/HTTP;
- logika OpenAI nie moze trafic do endpointow ani handlerow request-response;
- worker powinien byc hostowany jako `BackgroundService`, ale rekomendowanym ksztaltem jest osobny projekt `VibeTravels.Worker`, zgodny z `.ai/project_structure.md`.

Istotne luki w aktualnym repo, ktore plan musi domknac:
- brak projektu workerowego w solution;
- brak abstrakcji `IOpenAiClient` i brak implementacji integracji z OpenAI;
- brak encji persistence dla finalnego planu (`trip_plan` / `trip_plan_item` albo innego zaakceptowanego modelu planu);
- brak tabeli `trip_input_snapshot`, mimo ze plan queue endpointu juz do niej nawiazuje;
- `AiGenerationJob` ma dzis pola read-modelowe, ale nie ma jeszcze pelnego modelu zapisu odpowiedzi AI ani mechaniki retry/recovery;
- repo ma nazewnictwo statusow domenowych `Pending/Running`, podczas gdy API wystawia `queued/processing`; worker musi korzystac z jednego, jawnie ustalonego modelu mapowania.

Najwazniejsza niespojnosc specyfikacji do rozstrzygniecia przed kodowaniem:
- PRD i `analysis_for_prd_summary.md` opisuja plan jako tekst o stalej strukturze;
- `.ai/api_plan v2.md` zaklada `trip_plan` + `trip_plan_item` i odpowiedz JSON;
- rekomendacja: jako kanoniczny model zapisu przyjac nowszy kontrakt z `api_plan v2`, a surowy output AI trzymac opcjonalnie w `response_payload` joba do diagnostyki.

## 2. Konfiguracja workera
### Rekomendowany host
- dodac nowy projekt hosta, np. `VibeTravels.Worker`, do `VibeTravelers.sln`;
- projekt powinien referencjonowac `VibeTravels.Application` i `VibeTravels.Infrastructure`;
- worker uruchamia `BackgroundService`, nie Minimal API.

### Rejestracja DI
- `AddApplication()` i `AddInfrastructure(configuration)` wspoldzielone z API;
- rejestracja `HostedService`, np. `JobPollingHostedService`;
- rejestracja typed clienta / implementacji `IOpenAiClient`;
- osobny scope DI na kazdy przetwarzany job, z osobnym `DbContext`.

### Konfiguracja aplikacyjna
Dodac opcje konfiguracyjne, np.:
- `GenerationWorker:Enabled`
- `GenerationWorker:PollIntervalSeconds`
- `GenerationWorker:EmptyPollDelaySeconds`
- `GenerationWorker:BatchSize`
- `GenerationWorker:MaxParallelJobs`
- `GenerationWorker:MaxAttempts`
- `GenerationWorker:StaleProcessingAfterMinutes`
- `GenerationWorker:CommandTimeoutSeconds`
- `GenerationWorker:ShutdownTimeoutSeconds`
- `OpenAi:ApiKey`
- `OpenAi:BaseUrl` jesli client ma wspierac override endpointu
- `OpenAi:Model`
- `OpenAi:TimeoutSeconds`
- `OpenAi:MaxOutputTokens`
- `OpenAi:Temperature`

### Zasady operacyjne
- worker powinien miec mozliwosc wylaczenia przez config bez usuwania projektu;
- `ApiKey` i prompt templates nie moga trafic do logow ani do odpowiedzi HTTP;
- graceful shutdown powinien zatrzymac nowe pollingi, ale pozwolic dokonczyc in-flight job albo pozostawic go do recovery na starcie kolejnej instancji.

## 3. Model danych i logika zapisu joba
### Minimalny model persistence potrzebny workerowi
`AiGenerationJob` powinien przechowywac:
- `Id`
- `TripId`
- `UserId`
- `Status`
- `RequestedAt`
- `StartedAt`
- `FinishedAt`
- `AttemptNo`
- `ErrorCode`
- `ErrorMessage`
- `Discarded`
- `DiscardReason`
- `InputSnapshot` albo lepiej nazwane `RequestPayload`
- `ResponsePayload` jako opcjonalny raw output AI
- concurrency token (`xmin` albo jawna kolumna wersji) do optimistic concurrency

### Encje, ktorych dzis brakuje
- `TripInputSnapshot`
- `TripPlan`
- `TripPlanItem` jesli zespol przyjmuje model z `api_plan v2`

### Zasady zapisu po stronie queue endpointu
Queue handler i worker musza opierac sie na tym samym kontrakcie danych. Przy tworzeniu joba trzeba w jednej transakcji zapisac:
- rekord `AiGenerationJob` ze statusem `Pending` i `AttemptNo = 0`;
- snapshot inputu `before_generation`;
- pelny `request payload`, z ktorego worker odtworzy prompt po restarcie.

Payload zapisany przy kolejkowaniu powinien zawierac co najmniej:
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
- posortowana liste tagow z `tagId`, `code`, `displayName`, `order`

### Zasady zapisu po stronie workera
Po udanej generacji worker powinien w jednej transakcji:
- ponownie zaladowac job z blokada lub concurrency tokenem;
- sprawdzic, czy job nadal jest aktualny i nie zostal juz zakonczony przez inna instancje;
- wykonac check "newer job exists" dla tego samego `TripId`;
- jesli nowszy job istnieje, ustawic `Status = Succeeded`, `Discarded = true`, `DiscardReason = "newer_job_exists"` i nie zapisywac planu;
- jesli job jest nadal najnowszy, zapisac / nadpisac finalny plan;
- zaktualizowac `Trip.GeneratedAt`, `Trip.HasGeneratedPlan`, `Trip.UpdatedAt`;
- zapisac `TripInputSnapshot` typu `after_generation`, jesli zespol chce pelnej historii zgodnej z PRD;
- ustawic `Status = Succeeded`, `FinishedAt`, `ErrorCode = null`, `ErrorMessage = null`.

## 4. Flow przetwarzania danych
1. API zapisuje `AiGenerationJob` ze statusem `Pending` oraz payloadem wejsciowym.
2. `JobPollingHostedService` budzi sie co `PollIntervalSeconds`.
3. Worker probuje atomowo przejac batch jobow, np. przez `FOR UPDATE SKIP LOCKED`.
4. Przejety job przechodzi do `Running`, dostaje `StartedAt = now`, `AttemptNo = AttemptNo + 1`.
5. `GenerationJobProcessor` deserializuje `InputSnapshot` / `RequestPayload`.
6. Processor buduje prompt z danych snapshotu, nie z live state encji `Trip`.
7. `IOpenAiClient` wykonuje request do OpenAI z timeoutem i cancellation tokenem.
8. Worker waliduje odpowiedz:
   - czy JSON / struktura jest poprawna;
   - czy wymagane pola planu sa obecne;
   - czy liczba dni i daty sa spojne z output contract.
9. Worker otwiera transakcje zapisu wyniku.
10. Przed zapisem planu worker sprawdza, czy istnieje nowszy job dla tego samego tripa:
   - `RequestedAt > current.RequestedAt`
   - status w `Pending`, `Running` albo `Succeeded`
11. Jesli nowszy job istnieje, wynik obecnego joba jest odrzucany jako `discarded`.
12. Jesli job jest nadal aktualny, worker upsertuje plan i aktualizuje rekord `Trip`.
13. Worker oznacza job jako `Succeeded` albo `Failed`.
14. Gdy wystapi blad transient i sa jeszcze proby, worker przywraca job do `Pending`, zachowujac informacje o ostatnim bledzie.
15. Po wyczerpaniu retry job przechodzi do `Failed`.

### Rekomendowany algorytm claimowania
Samo `SELECT` + pozniejsze `UPDATE` nie wystarczy, jesli uruchomia sie dwie instancje workera. Potrzebny jest atomowy claim po stronie DB, np.:
- `SELECT ... FOR UPDATE SKIP LOCKED` po najstarszych `Pending`;
- potem `UPDATE ... SET status = 'running', started_at = now, attempt_no = attempt_no + 1`;
- najlepiej wykonane jednym poleceniem SQL `UPDATE ... FROM cte RETURNING ...`.

To podejscie pozwala:
- wspierac wiele instancji workera;
- uniknac podwojnego przetwarzania jednego joba;
- zachowac bounded parallelism bez zewnetrznego brokera.

## 5. Consistency, concurrency i recovery
### Zasady state machine
Rekomendowane przejscia:
- `Pending -> Running`
- `Running -> Succeeded`
- `Running -> Failed`
- `Running -> Pending` dla retry transientnych
- `Pending -> Canceled` tylko jesli zespol doda anulowanie

### Retry semantics
- `AttemptNo = 0` przy kolejkowaniu;
- kazdy realny start przetwarzania zwieksza `AttemptNo` o `1`;
- retry wykonuja sie tylko w workerze;
- retry dotycza tylko bledow transientnych: timeout, 429, 5xx, chwilowy blad sieci;
- bledy parsera, niezgodny output lub naruszenie invariantow planu powinny konczyc sie `Failed` bez kolejnej proby albo z osobna polityka `1` dodatkowej proby, jesli prompt potrafi produkowac przejsciowo zly JSON.

### Recovery po restarcie
Po starcie worker powinien najpierw wykonac recovery starych `Running` jobow:
- jesli `StartedAt` jest starsze niz `StaleProcessingAfterMinutes` i `FinishedAt == null`, job jest uznawany za porzucony;
- jesli `AttemptNo < MaxAttempts`, worker cofa go do `Pending`;
- w przeciwnym razie oznacza go jako `Failed` z czytelnym `ErrorCode`, np. `WORKER_STALE_JOB`.

### Regula "newer job wins"
Nawet jesli dzis API blokuje drugi aktywny job partial unique indexem, worker i tak powinien implementowac check nowszego joba przed zapisem planu. To daje:
- bezpieczny fallback na przyszlosc, jesli zespol zrezygnuje z `JOB_ALREADY_ACTIVE`;
- ochrone przed starym workerem / retry, ktory dochodzi do zapisu po pozniejszej generacji;
- zgodnosc z sekcja 7.4 w `.ai/api_plan v2.md`.

### Transakcyjnosc
W jednym commicie DB powinny byc laczone:
- zapis planu;
- aktualizacja `Trip`;
- finalizacja joba;
- opcjonalny `after_generation` snapshot.

Nie wolno rozdzielac zapisu planu od oznaczenia joba jako `Succeeded`, bo zostawi to system w stanie "plan istnieje, job dalej running/pending".

## 6. Implementacja integracji z OpenAI API
### Abstrakcje Application
Dodac w `VibeTravels.Application.Abstractions.Integrations`:
- `IOpenAiClient`
- opcjonalnie `ITripPlanPromptBuilder`
- opcjonalnie `IClock`, jesli worker ma miec testowalne znaczniki czasu

### Implementacja Infrastructure
Dodac w `VibeTravels.Infrastructure`:
- `Integrations/OpenAI/OpenAiClient.cs`
- `Integrations/OpenAI/Prompting/` dla buildera promptow i szablonow
- binding `OpenAiOptions`

### Rekomendowany kontrakt klienta
Worker powinien wywolywac OpenAI przez dedykowany model request-response, np.:
- wejscie: `TripPlanGenerationRequest`
- wyjscie: `TripPlanGenerationResult`

Ten kontrakt powinien byc odseparowany od:
- surowego payloadu HTTP do OpenAI;
- encji domenowych EF;
- endpointowych DTO.

### Prompting i format odpowiedzi
Rekomendacja dla MVP:
- uzyc jednego system promptu z twardymi regulami struktury;
- uzyc outputu JSON zgodnego z ustalonym schema planu;
- walidowac output po stronie workera przed zapisem;
- surowa odpowiedz AI moze trafic do `ResponsePayload` wylacznie do diagnostyki, nie do endpointow read model.

Jesli zespol pozostaje przy modelu tekstowym z PRD, worker nadal powinien:
- wymusic stala strukture sekcji;
- parsowac i walidowac wynik przed zapisem;
- zachowac raw text osobno od znormalizowanego modelu odczytu.

### Bezpieczenstwo integracji
- nie logowac promptu, pelnych notatek uzytkownika ani raw odpowiedzi modelu na poziomie `Information`;
- `ApiKey` tylko z konfiguracji / secret store;
- timeouty i retry po stronie `IOpenAiClient` musza byc ograniczone, aby job nie wisial bez konca;
- `CancellationToken` musi dochodzic az do requestu HTTP.

## 7. Obsluga bledow, observability i wydajnosc
### Kody bledow joba
Dodac stabilne `ErrorCode`, np.:
- `OPENAI_TIMEOUT`
- `OPENAI_RATE_LIMITED`
- `OPENAI_HTTP_ERROR`
- `OPENAI_INVALID_RESPONSE`
- `JOB_PAYLOAD_INVALID`
- `PLAN_PERSIST_FAILED`
- `WORKER_STALE_JOB`
- `NEWER_JOB_EXISTS`

### Logowanie
Logi powinny byc strukturalne i zawierac:
- `jobId`
- `tripId`
- `userId`
- `attemptNo`
- `traceId` / `correlationId`, jesli istnieja

Nie powinny zawierac:
- `noteText`
- prompt templates
- raw `request_payload` i `response_payload`
- kluczy API

### Metryki i health
Rekomendowane metryki:
- liczba pobranych jobow
- liczba sukcesow / faili / retry / discard
- sredni i p95 czas calla do OpenAI
- sredni i p95 czas calego joba
- liczba stalych `Running` jobow odzyskanych przy starcie

### Wydajnosc
- polling query musi korzystac z indeksu po `status, requested_at`;
- zachowac partial unique index jednego aktywnego joba na tripie, dopoki produkt nie potrzebuje wielu rownoleglych generacji;
- batch processing ograniczyc przez `MaxParallelJobs`, nie przez nieograniczone `Task.WhenAll`;
- nie wspoldzielic `DbContext` miedzy jobami;
- plan zapisywac przez projection / upsert, bez ladowania zbednych grafow encji;
- OpenAI client powinien byc reuzywalny (`HttpClient`/typed client), nie tworzony per request.

## 8. Kroki implementacji
1. Ustalic kanoniczny model finalnego planu
   - rozstrzygnac konflikt PRD vs `api_plan v2`;
   - rekomendacja: `trip_plan` + `trip_plan_item` jako model zapisu i odczytu.

2. Dodac projekt hosta workerowego
   - nowy projekt `VibeTravels.Worker`;
   - `Program.cs` z `Host.CreateApplicationBuilder`;
   - referencje do `Application` i `Infrastructure`;
   - rejestracja `JobPollingHostedService`.

3. Domknac modele persistence
   - dodac `TripInputSnapshot`, `TripPlan`, `TripPlanItem`;
   - rozszerzyc `AiGenerationJob` o `ResponsePayload` i concurrency token;
   - dodac konfiguracje EF, indeksy i migracje.

4. Uporzadkowac logike queue endpointu
   - zapis joba i snapshotu w jednej transakcji;
   - utrzymac kompletne `request payload`;
   - zostawic OpenAI poza handlerem HTTP.

5. Dodac abstrakcje integracyjne do Application
   - `IOpenAiClient`;
   - kontrakty request-response dla generacji planu;
   - opcjonalnie `ITripPlanPromptBuilder`, `IClock`.

6. Zaimplementowac OpenAI client w Infrastructure
   - typed client / `HttpClient`;
   - timeout, retry transientne, mapowanie bledow;
   - prompt builder i parser odpowiedzi.

7. Zaimplementowac mechanike claimowania jobow
   - atomowy claim `Pending -> Running`;
   - bounded parallelism;
   - osobny scope + `DbContext` per job.

8. Zaimplementowac `GenerationJobProcessor`
   - deserializacja payloadu;
   - call do OpenAI;
   - walidacja odpowiedzi;
   - transakcyjny zapis planu i finalizacja joba;
   - check `newer job exists`.

9. Dodac recovery i retry policy
   - reclaim starych `Running`;
   - `Running -> Pending` dla retry;
   - finalne `Failed` po wyczerpaniu prob.

10. Dodac observability
   - strukturalne logi;
   - stabilne `ErrorCode`;
   - podstawowe metryki i health checks.

11. Rozszerzyc docker / deployment
   - nowy serwis worker w `docker-compose.yml` albo osobny proces na VPS;
   - wspolna konfiguracja bazy i OpenAI;
   - mozliwosc niezaleznego restartu API i workera.

12. Dodac testy
   - unit: prompt builder, parser, retry classification, status transitions;
   - application/integration: claimowanie jobow, recovery `Running`, `newer job exists`, retry transientne;
   - end-to-end: queue endpoint zapisuje job, worker generuje plan, status endpoint pokazuje kolejne stany.


Komentarze architekta (nadpisują to co wyżej):
- odnośnie trip_input_snapshot to jest niepotrzebne ,  tabela generation_jobs ma kolumne input_snapshot
a encja w domenie AiGenerationJob ma kolumne AiGenerationJob